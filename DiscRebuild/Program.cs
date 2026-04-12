using System.Buffers.Binary;
using System.Text;

namespace DiscRebuild;

internal static class Program
{
    private const int BootBinSize = 0x440;
    private const int Bi2BinSize = 0x2000;
    private const int ApploaderOffset = 0x2440;
    private const int DolOffsetField = 0x420;
    private const int FstOffsetField = 0x424;
    private const int FstSizeField = 0x428;
    private const int FstMaxSizeField = 0x42C;
    private const int UserPositionField = 0x434;
    private const int UserLengthField = 0x438;
    private const int FileAlignment = 0x20;

    private static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            PrintUsage();
            return 1;
        }

        if (!args[0].StartsWith('-') && !args[0].StartsWith('/'))
            return RebuildDisc(args[0], args.Length >= 2 ? args[1] : null);

        string mode = args[0].TrimStart('-', '/').ToLowerInvariant();
        if (mode is "rebuild" or "gcrebuild")
            return RebuildDisc(RequireArg(args, 1), args.Length >= 3 ? args[2] : null);

        PrintUsage();
        return 1;
    }

    private static int RebuildDisc(string inputFolder, string? outputPath)
    {
        try
        {
            string sysDir = Path.Combine(inputFolder, "sys");
            string filesDir = Path.Combine(inputFolder, "files");

            RequireDirectory(inputFolder);
            RequireDirectory(sysDir);
            RequireDirectory(filesDir);

            string bootPath = Path.Combine(sysDir, "boot.bin");
            string bi2Path = Path.Combine(sysDir, "bi2.bin");
            string apploaderPath = Path.Combine(sysDir, "apploader.img");
            string dolPath = Path.Combine(sysDir, "main.dol");

            RequireFile(bootPath);
            RequireFile(bi2Path);
            RequireFile(apploaderPath);
            RequireFile(dolPath);

            outputPath ??= Path.Combine(
                Path.GetDirectoryName(inputFolder) ?? ".",
                Path.GetFileName(inputFolder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)) + ".iso");

            byte[] bootBin = File.ReadAllBytes(bootPath);
            byte[] bi2Bin = File.ReadAllBytes(bi2Path);
            byte[] apploader = File.ReadAllBytes(apploaderPath);
            byte[] mainDol = File.ReadAllBytes(dolPath);

            ValidateLength(bootBin, BootBinSize, "sys\\boot.bin");
            ValidateLength(bi2Bin, Bi2BinSize, "sys\\bi2.bin");

            DirectoryNode root = BuildTree(filesDir);
            List<FstEntryData> entries = new();
            List<byte> stringTable = new();
            AddDirectoryEntries(root, parentIndex: 0, entries, stringTable);

            uint dolOffset = Align((uint)(ApploaderOffset + apploader.Length), FileAlignment);
            uint fstOffset = Align((uint)(dolOffset + mainDol.Length), FileAlignment);
            byte[] fst = BuildFst(entries, stringTable, Align(fstOffset + 0u, 1u), fstOffset, mainDol.Length);
            uint fstSize = (uint)fst.Length;
            uint dataOffset = Align(fstOffset + fstSize, FileAlignment);
            AssignFileOffsets(entries, dataOffset);
            fst = BuildFst(entries, stringTable, dataOffset, fstOffset, mainDol.Length);
            fstSize = (uint)fst.Length;
            dataOffset = Align(fstOffset + fstSize, FileAlignment);
            AssignFileOffsets(entries, dataOffset);
            fst = BuildFst(entries, stringTable, dataOffset, fstOffset, mainDol.Length);
            fstSize = (uint)fst.Length;

            uint finalSize = dataOffset;
            foreach (FstEntryData entry in entries.Where(e => !e.IsDirectory))
            {
                finalSize = Align(entry.FileOffset + (uint)entry.FileSize, FileAlignment);
            }

            PatchBootBin(bootBin, dolOffset, fstOffset, fstSize, dataOffset, finalSize - dataOffset);

            using FileStream output = new(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
            WriteBlock(output, 0, bootBin);
            WriteBlock(output, BootBinSize, bi2Bin);
            WriteBlock(output, ApploaderOffset, apploader);
            WriteBlock(output, dolOffset, mainDol);
            WriteBlock(output, fstOffset, fst);

            foreach (FstEntryData entry in entries.Where(e => !e.IsDirectory))
            {
                Console.WriteLine($"[FILE] files/{entry.Path.Replace('\\', '/')}");
                WriteFile(output, entry.FileOffset, entry.SourcePath!);
            }

            output.SetLength(finalSize);
            Console.WriteLine($"Rebuild: {inputFolder} -> {outputPath}");
            Console.WriteLine("Finished Successfully!");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private static void PatchBootBin(byte[] bootBin, uint dolOffset, uint fstOffset, uint fstSize,
                                     uint userPosition, uint userLength)
    {
        WriteU32BE(bootBin, DolOffsetField, dolOffset);
        WriteU32BE(bootBin, FstOffsetField, fstOffset);
        WriteU32BE(bootBin, FstSizeField, fstSize);
        WriteU32BE(bootBin, FstMaxSizeField, fstSize);
        WriteU32BE(bootBin, UserPositionField, userPosition);
        WriteU32BE(bootBin, UserLengthField, userLength);
    }

    private static void AssignFileOffsets(List<FstEntryData> entries, uint startOffset)
    {
        uint currentOffset = startOffset;
        foreach (FstEntryData entry in entries.Where(e => !e.IsDirectory))
        {
            currentOffset = Align(currentOffset, FileAlignment);
            entry.FileOffset = currentOffset;
            currentOffset += (uint)entry.FileSize;
        }
    }

    private static byte[] BuildFst(List<FstEntryData> entries, List<byte> stringTable, uint dataOffset,
                                   uint fstOffset, int dolLength)
    {
        int totalSize = checked(entries.Count * 12 + stringTable.Count);
        byte[] fst = new byte[totalSize];
        for (int i = 0; i < entries.Count; i++)
        {
            FstEntryData entry = entries[i];
            uint nameField = (entry.IsDirectory ? 0x0100_0000u : 0u) | (uint)entry.NameOffset;
            WriteU32BE(fst, i * 12, nameField);
            WriteU32BE(fst, i * 12 + 4, entry.IsDirectory ? (uint)entry.ParentIndex : entry.FileOffset);
            WriteU32BE(fst, i * 12 + 8, entry.IsDirectory ? (uint)entry.NextIndex : (uint)entry.FileSize);
        }

        Buffer.BlockCopy(stringTable.ToArray(), 0, fst, entries.Count * 12, stringTable.Count);
        return fst;
    }

    private static void AddDirectoryEntries(DirectoryNode root, int parentIndex, List<FstEntryData> entries,
                                            List<byte> stringTable)
    {
        entries.Add(new FstEntryData
        {
            IsDirectory = true,
            NameOffset = 0,
            ParentIndex = 0,
            NextIndex = 0,
            Path = string.Empty
        });

        AddChildren(root, 0, entries, stringTable);
        entries[0].NextIndex = entries.Count;
    }

    private static void AddChildren(DirectoryNode directory, int parentIndex, List<FstEntryData> entries,
                                    List<byte> stringTable)
    {
        foreach (DirectoryNode childDir in directory.Directories)
        {
            int entryIndex = entries.Count;
            entries.Add(new FstEntryData
            {
                IsDirectory = true,
                NameOffset = AppendName(stringTable, childDir.Name),
                ParentIndex = parentIndex,
                Path = childDir.RelativePath
            });

            AddChildren(childDir, entryIndex, entries, stringTable);
            entries[entryIndex].NextIndex = entries.Count;
        }

        foreach (FileNode childFile in directory.Files)
        {
            entries.Add(new FstEntryData
            {
                IsDirectory = false,
                NameOffset = AppendName(stringTable, childFile.Name),
                FileSize = childFile.Size,
                SourcePath = childFile.SourcePath,
                Path = childFile.RelativePath
            });
        }
    }

    private static int AppendName(List<byte> stringTable, string name)
    {
        int offset = stringTable.Count;
        byte[] nameBytes = Encoding.ASCII.GetBytes(name);
        stringTable.AddRange(nameBytes);
        stringTable.Add(0);
        return offset;
    }

    private static DirectoryNode BuildTree(string rootPath)
    {
        DirectoryInfo rootInfo = new(rootPath);
        return BuildDirectory(rootInfo, string.Empty);
    }

    private static DirectoryNode BuildDirectory(DirectoryInfo directoryInfo, string relativePath)
    {
        DirectoryNode node = new(directoryInfo.Name, relativePath);

        foreach (DirectoryInfo childDir in directoryInfo.GetDirectories().OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase))
        {
            string childRelative = string.IsNullOrEmpty(relativePath) ? childDir.Name : Path.Combine(relativePath, childDir.Name);
            node.Directories.Add(BuildDirectory(childDir, childRelative));
        }

        foreach (FileInfo childFile in directoryInfo.GetFiles().OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase))
        {
            string childRelative = string.IsNullOrEmpty(relativePath) ? childFile.Name : Path.Combine(relativePath, childFile.Name);
            node.Files.Add(new FileNode(childFile.Name, childRelative, childFile.FullName, childFile.Length));
        }

        return node;
    }

    private static void WriteBlock(FileStream stream, uint offset, byte[] data)
    {
        stream.Position = offset;
        stream.Write(data, 0, data.Length);
    }

    private static void WriteFile(FileStream stream, uint offset, string sourcePath)
    {
        stream.Position = offset;
        using FileStream input = new(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        input.CopyTo(stream);
    }

    private static uint Align(uint value, uint alignment)
    {
        if (alignment == 0)
            return value;
        uint mask = alignment - 1;
        return (value + mask) & ~mask;
    }

    private static void WriteU32BE(byte[] data, int offset, uint value)
    {
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(offset, 4), value);
    }

    private static void ValidateLength(byte[] data, int expectedLength, string label)
    {
        if (data.Length != expectedLength)
            throw new InvalidOperationException($"{label} must be exactly 0x{expectedLength:X} bytes.");
    }

    private static void RequireFile(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"File not found: {path}");
    }

    private static void RequireDirectory(string path)
    {
        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException($"Folder not found: {path}");
    }

    private static string RequireArg(string[] args, int index)
    {
        if (index >= args.Length)
            throw new InvalidOperationException("Missing required input folder.");
        return args[index];
    }

    private static void PrintUsage()
    {
        Console.WriteLine("DiscRebuild");
        Console.WriteLine("  GameCube ISO/GCM rebuilder for Hocotate Toolkit");
        Console.WriteLine();
        Console.WriteLine("Credits");
        Console.WriteLine("  Referenced from Dolphin / DolphinTool");
        Console.WriteLine("  Author reference: jordan-woyak");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  DiscRebuild.exe <folder> [output.iso]");
        Console.WriteLine("  DiscRebuild.exe --rebuild <folder> [output.iso]");
        Console.WriteLine();
        Console.WriteLine("Input layout:");
        Console.WriteLine("  folder\\sys\\boot.bin");
        Console.WriteLine("  folder\\sys\\bi2.bin");
        Console.WriteLine("  folder\\sys\\apploader.img");
        Console.WriteLine("  folder\\sys\\main.dol");
        Console.WriteLine("  folder\\files\\...");
    }

    private sealed class DirectoryNode(string name, string relativePath)
    {
        public string Name { get; } = name;
        public string RelativePath { get; } = relativePath;
        public List<DirectoryNode> Directories { get; } = new();
        public List<FileNode> Files { get; } = new();
    }

    private sealed class FileNode(string name, string relativePath, string sourcePath, long size)
    {
        public string Name { get; } = name;
        public string RelativePath { get; } = relativePath;
        public string SourcePath { get; } = sourcePath;
        public long Size { get; } = size;
    }

    private sealed class FstEntryData
    {
        public bool IsDirectory { get; init; }
        public int NameOffset { get; init; }
        public int ParentIndex { get; init; }
        public int NextIndex { get; set; }
        public uint FileOffset { get; set; }
        public long FileSize { get; init; }
        public string? SourcePath { get; init; }
        public string Path { get; init; } = string.Empty;
    }
}
