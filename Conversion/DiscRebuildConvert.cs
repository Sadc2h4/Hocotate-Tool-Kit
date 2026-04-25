using System;
using System.IO;

namespace RARCToolkit.Conversion
{
    public static class DiscRebuildConvert
    {
        public static int RebuildDisc(string inputFolder, string? outputPath)
        {
            if (!Directory.Exists(inputFolder))
                throw new DirectoryNotFoundException($"Folder not found: {inputFolder}");

            inputFolder = Path.GetFullPath(inputFolder);
            string exePath = ExeRunner.FindExe("DiscRebuild.exe");

            outputPath ??= Path.Combine(
                Path.GetDirectoryName(inputFolder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)) ?? ".",
                Path.GetFileName(inputFolder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)) + ".iso");
            outputPath = Path.GetFullPath(outputPath);

            return ExeRunner.Run(exePath, new[] { "--rebuild", inputFolder, outputPath });
        }

        public static int RebuildGameCubeDisc(string inputFolder, string? outputPath)
        {
            outputPath ??= Path.Combine(
                Path.GetDirectoryName(inputFolder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)) ?? ".",
                Path.GetFileName(inputFolder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)) + ".iso");
            return RebuildDisc(inputFolder, outputPath);
        }

        public static int RebuildWiiDisc(string inputFolder, string? outputPath)
        {
            outputPath ??= Path.Combine(
                Path.GetDirectoryName(inputFolder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)) ?? ".",
                Path.GetFileName(inputFolder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)) + ".wbfs");
            return RebuildDisc(inputFolder, outputPath);
        }

        public static int ConvertIsoToWbfs(string inputIso, string? outputPath)
        {
            if (!File.Exists(inputIso))
                throw new FileNotFoundException($"File not found: {inputIso}");

            inputIso = Path.GetFullPath(inputIso);
            string exePath = ExeRunner.FindExe("DiscRebuild.exe");

            outputPath ??= Path.Combine(
                Path.GetDirectoryName(inputIso) ?? ".",
                Path.GetFileNameWithoutExtension(inputIso) + ".wbfs");
            outputPath = Path.GetFullPath(outputPath);

            return ExeRunner.Run(exePath, new[] { "--iso2wbfs", inputIso, outputPath });
        }
    }
}
