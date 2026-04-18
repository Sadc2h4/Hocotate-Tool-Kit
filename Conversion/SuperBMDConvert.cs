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

            Console.WriteLine($"  BMD → DAE: {Path.GetFileName(inputBmd)}");
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
            inputDae = Path.GetFullPath(inputDae);

            if (string.IsNullOrEmpty(outputBmd))
            {
                string dir  = Path.GetDirectoryName(inputDae) ?? ".";
                string name = Path.GetFileNameWithoutExtension(inputDae);
                outputBmd   = Path.Combine(dir, name + ".bmd");
            }
            outputBmd = Path.GetFullPath(outputBmd);
            if (!string.IsNullOrEmpty(materialsJson))
                materialsJson = Path.GetFullPath(materialsJson);
            if (!string.IsNullOrEmpty(texHeaderJson))
                texHeaderJson = Path.GetFullPath(texHeaderJson);

            var args = new List<string> { inputDae, outputBmd };
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

            Console.WriteLine($"  DAE → BMD: {Path.GetFileName(inputDae)}");
            int code = ExeRunner.Run(GetSuperBmdExe(), args, Path.GetDirectoryName(inputDae));
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

            Console.WriteLine($"  BMD → OBJ: {Path.GetFileName(inputBmd)}");
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

            Console.WriteLine($"  BMD → FBX: {Path.GetFileName(inputBmd)} → {outputDir}");
            int code = ExeRunner.Run(bmd2fbxExe, args, Path.GetDirectoryName(bmd2fbxExe));
            return code;
        }

        // ─── ヘルパー ────────────────────────────────────────────────

        private static void ValidateInput(string path, params string[] allowedExts)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"入力ファイルが見つかりません: {path}");

            string ext = Path.GetExtension(path).ToLower();
            bool ok = false;
            foreach (string e in allowedExts) if (e == ext) { ok = true; break; }
            if (!ok)
                throw new ArgumentException(
                    $"入力ファイルの拡張子が正しくありません: {ext}\n" +
                    $"受け付ける拡張子: {string.Join(", ", allowedExts)}");
        }
    }
}
