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

            string exePath = ExeRunner.FindExe("DiscExtract.exe");

            outputDir ??= Path.Combine(
                Path.GetDirectoryName(inputPath) ?? ".",
                Path.GetFileNameWithoutExtension(inputPath));

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
