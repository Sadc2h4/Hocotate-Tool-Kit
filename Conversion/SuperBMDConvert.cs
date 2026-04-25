using System;
using System.Collections.Generic;
using System.IO;

namespace RARCToolkit.Conversion
{
    /// <summary>
    /// BMD_analysis.exe / FBX_analysis.exe を内部的に呼び出して BMD/DAE/OBJ/FBX 変換を行うラッパー。
    /// 各 exe は Hocotate_Toolkit.exe と同じ階層の resource\ フォルダに配置してください。
    /// </summary>
    public static class SuperBMDConvert
    {
        private static string? _superBmdExe;

        private static string GetSuperBmdExe()
            => _superBmdExe ??= ExeRunner.FindExe("BMD_analysis.exe");

        // ─── BMD → DAE ──────────────────────────────────────────────

        /// <summary>
        /// BMD/BDL ファイルを Collada (.dae) に変換します。
        /// </summary>
        /// <param name="inputBmd">入力 .bmd ファイルパス</param>
        /// <param name="outputDae">出力 .dae パス（省略時は入力と同じフォルダ）</param>
        /// <returns>成功なら 0</returns>
        public static int Bmd2Dae(string inputBmd, string? outputDae)
        {
            ValidateInput(inputBmd, ".bmd", ".bdl");
            inputBmd = Path.GetFullPath(inputBmd);
            if (!string.IsNullOrEmpty(outputDae))
                outputDae = Path.GetFullPath(outputDae);

            var args = new List<string> { inputBmd };
            if (!string.IsNullOrEmpty(outputDae))
                args.Add(outputDae);

            Console.WriteLine($"  BMD -> DAE: {Path.GetFileName(inputBmd)}");
            int code = ExeRunner.Run(GetSuperBmdExe(), args, Path.GetDirectoryName(inputBmd));
            return code;
        }

        // ─── DAE → BMD ──────────────────────────────────────────────

        /// <summary>
        /// Collada (.dae) ファイルを BMD に変換します。
        /// </summary>
        /// <param name="inputDae">入力 .dae ファイルパス</param>
        /// <param name="outputBmd">出力 .bmd パス（省略時は入力と同じフォルダ）</param>
        /// <param name="materialsJson">マテリアル定義 JSON パス（オプション）</param>
        /// <param name="texHeaderJson">テクスチャヘッダー JSON パス（オプション）</param>
        /// <returns>成功なら 0</returns>
        public static int Dae2Bmd(string inputDae, string? outputBmd,
                                   string? materialsJson, string? texHeaderJson)
        {
            ValidateInput(inputDae, ".dae");
            Console.WriteLine($"  DAE -> BMD: {Path.GetFileName(inputDae)}");
            return Model2Bmd(inputDae, outputBmd, materialsJson, texHeaderJson);
        }

        /// <summary>
        /// Assimp supported model files (.dae/.fbx) を BMD に変換します。
        /// スケルトンルート 'skeleton_root' が見つからない場合は --noskeleton で自動リトライします。
        /// </summary>
        public static int Model2Bmd(string inputModel, string? outputBmd,
                                    string? materialsJson, string? texHeaderJson)
        {
            ValidateInput(inputModel, ".dae", ".fbx");
            inputModel = Path.GetFullPath(inputModel);

            if (string.IsNullOrEmpty(outputBmd))
            {
                string dir  = Path.GetDirectoryName(inputModel) ?? ".";
                string name = Path.GetFileNameWithoutExtension(inputModel);
                outputBmd   = Path.Combine(dir, name + ".bmd");
            }
            outputBmd = Path.GetFullPath(outputBmd);
            if (!string.IsNullOrEmpty(materialsJson))
                materialsJson = Path.GetFullPath(materialsJson);
            if (!string.IsNullOrEmpty(texHeaderJson))
                texHeaderJson = Path.GetFullPath(texHeaderJson);

            var args = new List<string> { inputModel, outputBmd };
            if (!string.IsNullOrEmpty(materialsJson))
            {
                args.Add("--mat");
                args.Add(materialsJson);
            }
            if (!string.IsNullOrEmpty(texHeaderJson))
            {
                args.Add("--texheader");
                args.Add(texHeaderJson);
            }

            string? workDir = Path.GetDirectoryName(inputModel);
            var capturedStderr = new System.Text.StringBuilder();
            int code = ExeRunner.Run(GetSuperBmdExe(), args, workDir, capturedStderr);

            // SuperBMD がスケルトンルート未検出でクラッシュした場合、--noskeleton で再試行する。
            // 外部FBXや古い bmd2fbx 出力など、SuperBMD が要求する 'skeleton_root' ノードが
            // 存在しないモデルをスタティックメッシュとして扱うためのフォールバック。
            if (code != 0 &&
                capturedStderr.ToString().Contains("skeleton root has not been found",
                                                   StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine();
                Console.WriteLine("[Warning] The model does not contain a 'skeleton_root' node.");
                Console.WriteLine("          Retrying as a static mesh (--noskeleton)...");
                Console.WriteLine("          For rigged models, rename the armature/root node to 'skeleton_root'");
                Console.WriteLine("          in your 3D application before converting.");
                Console.WriteLine();
                args.Add("--noskeleton");
                code = ExeRunner.Run(GetSuperBmdExe(), args, workDir);
            }

            return code;
        }

        // ─── BMD → OBJ ──────────────────────────────────────────────

        /// <summary>
        /// BMD/BDL ファイルを Wavefront OBJ に変換します。
        /// </summary>
        /// <param name="inputBmd">入力 .bmd ファイルパス</param>
        /// <param name="outputObj">出力 .obj パス（省略時は入力と同じフォルダ）</param>
        /// <returns>成功なら 0</returns>
        public static int Bmd2Obj(string inputBmd, string? outputObj)
        {
            ValidateInput(inputBmd, ".bmd", ".bdl");
            inputBmd = Path.GetFullPath(inputBmd);
            if (!string.IsNullOrEmpty(outputObj))
                outputObj = Path.GetFullPath(outputObj);

            var args = new List<string> { inputBmd };
            if (!string.IsNullOrEmpty(outputObj))
                args.Add(outputObj);
            args.Add("--exportobj");

            Console.WriteLine($"  BMD -> OBJ: {Path.GetFileName(inputBmd)}");
            int code = ExeRunner.Run(GetSuperBmdExe(), args, Path.GetDirectoryName(inputBmd));
            return code;
        }

        // ─── BMD → FBX ──────────────────────────────────────────────

        /// <summary>
        /// BMD ファイルを FBX に変換します。
        /// FBX_analysis.exe を resource\ フォルダに配置する必要があります。
        /// </summary>
        /// <param name="inputBmd">入力 .bmd ファイルパス</param>
        /// <param name="outputDir">出力フォルダ（省略時は入力と同じ場所に <name>/ フォルダを作成）</param>
        /// <returns>成功なら 0</returns>
        public static int Bmd2Fbx(string inputBmd, string? outputDir)
        {
            ValidateInput(inputBmd, ".bmd", ".bdl");
            inputBmd = Path.GetFullPath(inputBmd);
            string bmd2fbxExe = ExeRunner.FindExe("FBX_analysis.exe");

            if (string.IsNullOrEmpty(outputDir))
            {
                string dir  = Path.GetDirectoryName(inputBmd) ?? ".";
                string name = Path.GetFileNameWithoutExtension(inputBmd);
                outputDir   = Path.Combine(dir, name);
            }
            outputDir = Path.GetFullPath(outputDir);
            Directory.CreateDirectory(outputDir);

            var args = new List<string>
            {
                "manual",
                "--bmd", inputBmd,
                "--out", outputDir,
                "--static",
            };

            Console.WriteLine($"  BMD -> FBX: {Path.GetFileName(inputBmd)} -> {outputDir}");
            int code = ExeRunner.Run(bmd2fbxExe, args, Path.GetDirectoryName(bmd2fbxExe));
            return code;
        }

        // ─── ヘルパー ────────────────────────────────────────────────

        private static void ValidateInput(string path, params string[] allowedExts)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Input file not found: {path}");

            string ext = Path.GetExtension(path).ToLower();
            bool ok = false;
            foreach (string e in allowedExts) if (e == ext) { ok = true; break; }
            if (!ok)
                throw new ArgumentException(
                    $"Unsupported input file extension: {ext}\n" +
                    $"Accepted extensions: {string.Join(", ", allowedExts)}");
        }
    }
}
