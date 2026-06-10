using System;
using System.IO;

namespace RARCToolkit.Conversion
{
    public static class DiscExtractConvert
    {
        public static int ExtractDiscImage(string inputPath, string? outputDir)
        {
            if (!File.Exists(inputPath))
                throw new FileNotFoundException($"File not found: {inputPath}");

            inputPath = Path.GetFullPath(inputPath);
            // Linux版は拡張子なしのバイナリ名で探す（resource/DiscExtract）
            string exePath = ExeRunner.FindExe("DiscExtract");

            outputDir ??= Path.Combine(
                Path.GetDirectoryName(inputPath) ?? ".",
                Path.GetFileNameWithoutExtension(inputPath));
            outputDir = Path.GetFullPath(outputDir);

            return ExeRunner.Run(exePath, new[] { "--extract", inputPath, outputDir });
        }

        public static int ExtractGameCubeDisc(string inputIso, string? outputDir)
        {
            return ExtractDiscImage(inputIso, outputDir);
        }

        public static int ExtractWiiDisc(string inputImage, string? outputDir)
        {
            return ExtractDiscImage(inputImage, outputDir);
        }
    }
}
