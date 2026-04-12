using System;
using System.IO;

namespace RARCToolkit.Conversion
{
    public static class DiscRebuildConvert
    {
        public static int RebuildGameCubeDisc(string inputFolder, string? outputPath)
        {
            if (!Directory.Exists(inputFolder))
                throw new DirectoryNotFoundException($"Folder not found: {inputFolder}");

            string exePath = ExeRunner.FindExe("DiscRebuild.exe");

            outputPath ??= Path.Combine(
                Path.GetDirectoryName(inputFolder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)) ?? ".",
                Path.GetFileName(inputFolder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)) + ".iso");

            return ExeRunner.Run(exePath, new[] { "--rebuild", inputFolder, outputPath });
        }
    }
}
