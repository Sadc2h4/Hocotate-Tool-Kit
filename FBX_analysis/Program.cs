using System;
using System.IO;
using System.Linq;

using Assimp;

using fin.io;
using fin.model.io.exporters;
using fin.model.io.exporters.assimp.indirect;
using fin.model.io.exporters.gltf;

using jsystem.api;


namespace fbx_analysis;

//-------------------------------------------------------------------------------
// BMD ファイルを FBX に変換する CLI エントリーポイント
//-------------------------------------------------------------------------------
internal static class Program {
  private const string AppName = "FBX_analysis v2";

  static int Main(string[] args) {
    Console.WriteLine(AppName);
    Console.WriteLine();

    if (args.Length < 1) {
      PrintUsage();
      return 1;
    }

    //-------------------------------------------------------------------------------
    // "manual" モード: --bmd <path> --out <dir> [--static] を解析する
    //-------------------------------------------------------------------------------
    if (args[0].Equals("manual", StringComparison.OrdinalIgnoreCase)) {
      return RunManualMode(args[1..]);
    }

    //-------------------------------------------------------------------------------
    // シンプルモード: <bmd_path> <output_dir>
    //-------------------------------------------------------------------------------
    if (args.Length >= 2) {
      return RunConvert(args[0], args[1]);
    }

    PrintUsage();
    return 1;
  }

  //-------------------------------------------------------------------------------
  // --bmd / --out フラグを解析して変換を実行する
  //-------------------------------------------------------------------------------
  private static int RunManualMode(string[] args) {
    string? bmdPath = null;
    string? outDir  = null;

    for (var i = 0; i < args.Length; i++) {
      switch (args[i].ToLowerInvariant()) {
        case "--bmd":
          if (i + 1 < args.Length) bmdPath = args[++i];
          break;
        case "--out":
          if (i + 1 < args.Length) outDir = args[++i];
          break;
        case "--static":
          break; // 静的メッシュのみ（アニメーションファイル未指定と同等）
      }
    }

    if (bmdPath == null || outDir == null) {
      Console.Error.WriteLine("Error: --bmd and --out are required in manual mode.");
      PrintUsage();
      return 1;
    }

    return RunConvert(bmdPath, outDir);
  }

  //-------------------------------------------------------------------------------
  // BMD ファイルを読み込んで FBX（ASCII 形式・skeleton_root 付き）を出力する
  //
  // エクスポートパイプライン:
  //   BMD → IModel → 一時 GLB → Assimp シーン
  //     → skeleton_root ノード挿入 → ASCII FBX 出力
  //
  // ASCII FBX を使用する理由:
  //   BMD_analysis.exe 内部の Assimp 3.3.1 (2016) は Assimp 5.x が書き出す
  //   FBX バイナリ v7600 を読めない。ASCII FBX はバージョン非依存。
  //
  // skeleton_root を挿入する理由:
  //   BMD_analysis.exe (SuperBMD) は FBX→BMD 変換時にシーン直下の
  //   "skeleton_root" ダミーノードを必須とする。このノードが無いと
  //   ボーン情報が失われた状態でBMD化される。
  //-------------------------------------------------------------------------------
  private static int RunConvert(string bmdPath, string outputDir) {
    bmdPath   = Path.GetFullPath(bmdPath);
    outputDir = Path.GetFullPath(outputDir);

    if (!File.Exists(bmdPath)) {
      Console.Error.WriteLine($"Error: File not found: {bmdPath}");
      return 1;
    }

    Directory.CreateDirectory(outputDir);

    var modelName  = Path.GetFileNameWithoutExtension(bmdPath);
    var outputPath = Path.Combine(outputDir, modelName + ".fbx");
    // GLB は旧 FBX_analysis.exe に合わせて "_gltf.glb" として保持する
    var glbPath    = Path.Combine(outputDir, modelName + "_gltf.glb");

    Console.WriteLine($"Input : {Path.GetFileName(bmdPath)}");
    Console.WriteLine($"Output: {outputPath}");
    Console.WriteLine();

    try {
      Console.WriteLine("Importing BMD...");
      var bundle = new BmdModelFileBundle { BmdFile = new FinFile(bmdPath) };
      var model  = new BmdModelImporter().Import(bundle);

      Console.WriteLine("Exporting GLB...");

      //-------------------------------------------------------------------------------
      // IModel を GLB にエクスポートする
      // "_gltf.glb" として保存し Blender 等での利用を可能にする
      //-------------------------------------------------------------------------------
      var gltfExporter = new GltfModelExporter { UvIndices = true, Embedded = true };
      gltfExporter.ExportModel(new ModelExporterParams {
          Model      = model,
          OutputFile = new FinFile(glbPath),
          Scale      = 100,
      });

      Console.WriteLine("Converting GLB -> FBX (ASCII) with skeleton_root...");

      using var ctx = new AssimpContext();
      var assScene  = ctx.ImportFile(glbPath);

      //-------------------------------------------------------------------------------
      // UV・アニメーション・テクスチャの補正を適用する
      //-------------------------------------------------------------------------------
      AssimpIndirectAnimationFixer.Fix(model, assScene);
      AssimpIndirectUvFixer.Fix(model, assScene);
      AssimpIndirectTextureFixer.Fix(model, assScene);

      //-------------------------------------------------------------------------------
      // シーン直下のボーン系ノードを "skeleton_root" でラップする
      // SuperBMD は RootNode の直下に "skeleton_root" ノードがあることを前提とする
      //-------------------------------------------------------------------------------
      InsertSkeletonRoot(assScene);

      //-------------------------------------------------------------------------------
      // ASCII FBX でエクスポートする（旧 Assimp との互換性を確保）
      //-------------------------------------------------------------------------------
      var success = ctx.ExportFile(assScene, outputPath, "fbxa");
      if (!success) {
        Console.Error.WriteLine("Error: FBX export failed.");
        return 1;
      }

      Console.WriteLine("Done.");
      return 0;
    } catch (Exception ex) {
      Console.Error.WriteLine($"Error: {ex.Message}");
      return 1;
    }
  }

  //-------------------------------------------------------------------------------
  // Assimp シーンの RootNode 直下に "skeleton_root" ダミーノードを挿入する
  // メッシュを持たない上位ノード（ボーン系ノード）を skeleton_root の子に移動する
  //-------------------------------------------------------------------------------
  private static void InsertSkeletonRoot(Scene scene) {
    // 既に skeleton_root があればスキップ
    if (scene.RootNode.Children.Any(c => c.Name == "skeleton_root")) {
      return;
    }

    // ボーン名の一覧を収集する（各メッシュの Bone リストから）
    var boneNames = new System.Collections.Generic.HashSet<string>(
        scene.Meshes
             .Where(m => m.HasBones)
             .SelectMany(m => m.Bones)
             .Select(b => b.Name),
        StringComparer.Ordinal);

    if (boneNames.Count == 0) {
      return; // ボーンなし → skeleton_root 不要
    }

    // RootNode の直下でメッシュを持たないノード = ボーン階層のルート候補
    var boneRootCandidates = scene.RootNode.Children
        .Where(c => !c.HasMeshes && ContainsBone(c, boneNames))
        .ToList();

    if (boneRootCandidates.Count == 0) {
      return;
    }

    // skeleton_root ダミーノードを作成してボーンルートを移動する
    var skeletonRoot = new Node("skeleton_root");

    foreach (var node in boneRootCandidates) {
      scene.RootNode.Children.Remove(node);
      skeletonRoot.Children.Add(node);
    }

    scene.RootNode.Children.Add(skeletonRoot);

    Console.WriteLine($"  skeleton_root: wrapped {boneRootCandidates.Count} node(s)");
    foreach (var n in boneRootCandidates) {
      Console.WriteLine($"    -> {n.Name}");
    }
  }

  //-------------------------------------------------------------------------------
  // 指定ノードまたはその子孫にボーン名が含まれるか再帰的に確認する
  //-------------------------------------------------------------------------------
  private static bool ContainsBone(
      Node node,
      System.Collections.Generic.HashSet<string> boneNames) {
    if (boneNames.Contains(node.Name)) return true;
    return node.Children.Any(c => ContainsBone(c, boneNames));
  }

  //-------------------------------------------------------------------------------
  // 使い方を表示する
  //-------------------------------------------------------------------------------
  private static void PrintUsage() {
    Console.WriteLine("Usage:");
    Console.WriteLine("  FBX_analysis.exe manual --bmd <input.bmd> --out <output_dir> [--static]");
    Console.WriteLine("  FBX_analysis.exe <input.bmd> <output_dir>");
  }
}
