using System;
using System.IO;

namespace RARCToolkit.Conversion
{
    public static class DiscExtractConvert
    {
        public static int ExtractGameCubeDisc(string inputIso, string? outputDir)
        {
            if (!File.Exists(inputIso))
                throw new FileNotFoundException($"File not found: {inputIso}");

            string exePath = ExeRunner.FindExe("DiscExtract.exe");

            outputDir ??= Path.Combine(
                Path.GetDirectoryName(inputIso) ?? ".",
                Path.GetFileNameWithoutExtension(inputIso));

            return ExeRunner.Run(exePath, new[] { "--extract", inputIso, outputDir });
        }
    }
}
