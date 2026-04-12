using System.Buffers.Binary;
using System.Text;

namespace DiscExtract;

internal static class Program
{
    private const int BootBinSize = 0x440;
    private const int Bi2BinOffset = 0x440;
    private const int Bi2BinSize = 0x2000;
    private const int ApploaderOffset = 0x2440;
    private const int DolOffsetField = 0x420;
    private const int FstOffsetField = 0x424;
    private const int FstSizeField = 0x428;

    private static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            PrintUsage();
            return 1;
        }

        if (!args[0].StartsWith('-') && !args[0].StartsWith('/'))
            return ExtractDisc(args[0], args.Length >= 2 ? args[1] : null);

        string mode = args[0].TrimStart('-', '/').ToLowerInvariant();
        if (mode is "extract" or "gcextract")
            return ExtractDisc(RequireArg(args, 1), args.Length >= 3 ? args[2] : null);

        PrintUsage();
        return 1;
    }

    private static int ExtractDisc(string inputPath, string? outputDir)
    {
        try
        {
            RequireFile(inputPath);
            string ext = Path.GetExtension(inputPath).ToLowerInvariant();
            if (ext is not ".iso" and not ".gcm")
                throw new InvalidOperationException("Supported formats: .iso, .gcm");

            outputDir ??= Path.Combine(
                Path.GetDirectoryName(inputPath) ?? ".",
                Path.GetFileNameWithoutExtension(inputPath));

            Directory.CreateDirectory(outputDir);

            using FileStream stream = new(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using BinaryReader reader = new(stream, Encoding.ASCII, leaveOpen: true);

            uint dolOffset = ReadU32BE(reader, DolOffsetField);
            uint fstOffset = ReadU32BE(reader, FstOffsetField);
            uint fstSize = ReadU32BE(reader, FstSizeField);

            string sysDir = Path.Combine(outputDir, "sys");
            string filesDir = Path.Combine(outputDir, "files");
            Directory.CreateDirectory(sysDir);
            Directory.CreateDirectory(filesDir);

            WriteRegion(stream, 0, BootBinSize, Path.Combine(sysDir, "boot.bin"));
            WriteRegion(stream, Bi2BinOffset, Bi2BinSize, Path.Combine(sysDir, "bi2.bin"));
            WriteRegion(stream, ApploaderOffset, GetApploaderSize(reader), Path.Combine(sysDir, "apploader.img"));
            WriteRegion(stream, dolOffset, GetDolSize(reader, dolOffset), Path.Combine(sysDir, "main.dol"));
            WriteRegion(stream, fstOffset, fstSize, Path.Combine(sysDir, "fst.bin"));

            Console.WriteLine($"Extract: {inputPath} -> {outputDir}");
            ExtractFilesystem(stream, fstOffset, fstSize, filesDir);
            Console.WriteLine("Finished Successfully!");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private static void ExtractFilesystem(FileStream stream, uint fstOffset, uint fstSize, string filesDir)
    {
        stream.Position = fstOffset;
        byte[] fst = ReadExact(stream, checked((int)fstSize));
        FstEntry[] entries = ParseFst(fst);
        if (entries.Length == 0)
            throw new InvalidOperationException("FST is empty.");

        int rootEnd = checked((int)entries[0].Size);
        ExtractDirectory(stream, entries, 0, 1, rootEnd, filesDir);
    }

    private static int ExtractDirectory(FileStream stream, FstEntry[] entries, int directoryIndex,
                                        int childIndex, int endIndex, string currentOutputDir)
    {
        Directory.CreateDirectory(currentOutputDir);

        int index = childIndex;
        while (index < endIndex)
        {
            FstEntry entry = entries[index];
            string outputPath = Path.Combine(currentOutputDir, entry.Name);

            if (entry.IsDirectory)
            {
                Console.WriteLine($"[DIR ] {GetDisplayPath(outputPath)}");
                index = ExtractDirectory(stream, entries, index, index + 1, checked((int)entry.Size), outputPath);
            }
            else
            {
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
                Console.WriteLine($"[FILE] {GetDisplayPath(outputPath)}");
                WriteRegion(stream, entry.Offset, entry.Size, outputPath);
                index++;
            }
        }

        return directoryIndex == 0 ? endIndex : index;
    }

    private static string GetDisplayPath(string outputPath)
    {
        return outputPath.Replace(Path.DirectorySeparatorChar, '/');
    }

    private static FstEntry[] ParseFst(byte[] fst)
    {
        if (fst.Length < 12)
            throw new InvalidOperationException("Invalid FST size.");

        uint rootSize = ReadU32BE(fst, 8);
        int entryCount = checked((int)rootSize);
        int namesOffset = checked(entryCount * 12);
        if (fst.Length < namesOffset)
            throw new InvalidOperationException("Invalid FST layout.");

        FstEntry[] entries = new FstEntry[entryCount];
        for (int i = 0; i < entryCount; i++)
        {
            int entryOffset = i * 12;
            uint nameField = ReadU32BE(fst, entryOffset);
            bool isDirectory = (nameField & 0xFF00_0000) != 0;
            int nameOffset = checked((int)(nameField & 0x00FF_FFFF));
            string name = i == 0 ? string.Empty : ReadCString(fst, namesOffset + nameOffset);
            uint offset = ReadU32BE(fst, entryOffset + 4);
            uint size = ReadU32BE(fst, entryOffset + 8);
            entries[i] = new FstEntry(name, isDirectory, offset, size);
        }

        return entries;
    }

    private static uint GetApploaderSize(BinaryReader reader)
    {
        uint codeSize = ReadU32BE(reader, ApploaderOffset + 0x14);
        uint trailerSize = ReadU32BE(reader, ApploaderOffset + 0x18);
        return checked((uint)(0x20 + codeSize + trailerSize));
    }

    private static uint GetDolSize(BinaryReader reader, uint dolOffset)
    {
        const int textSectionCount = 7;
        const int dataSectionCount = 11;
        const int sectionSizeTableOffset = 0x90;
        const int headerSize = 0x100;

        uint maxEnd = headerSize;
        for (int i = 0; i < textSectionCount + dataSectionCount; i++)
        {
            uint sectionOffset = ReadU32BE(reader, checked((int)(dolOffset + (uint)(i * 4))));
            uint sectionSize = ReadU32BE(reader, checked((int)(dolOffset + sectionSizeTableOffset + i * 4)));
            if (sectionOffset == 0 || sectionSize == 0)
                continue;

            uint end = checked(sectionOffset + sectionSize);
            if (end > maxEnd)
                maxEnd = end;
        }

        return maxEnd;
    }

    private static void WriteRegion(FileStream stream, uint offset, uint size, string outputPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        stream.Position = offset;

        using FileStream output = new(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
        byte[] buffer = new byte[1024 * 1024];
        uint remaining = size;
        while (remaining > 0)
        {
            int chunk = (int)Math.Min((uint)buffer.Length, remaining);
            int read = stream.Read(buffer, 0, chunk);
            if (read != chunk)
                throw new EndOfStreamException($"Unexpected end of file while writing {outputPath}");

            output.Write(buffer, 0, read);
            remaining -= (uint)read;
        }
    }

    private static byte[] ReadExact(FileStream stream, int size)
    {
        byte[] buffer = new byte[size];
        int readTotal = 0;
        while (readTotal < size)
        {
            int read = stream.Read(buffer, readTotal, size - readTotal);
            if (read == 0)
                throw new EndOfStreamException("Unexpected end of file.");
            readTotal += read;
        }

        return buffer;
    }

    private static uint ReadU32BE(BinaryReader reader, int offset)
    {
        reader.BaseStream.Position = offset;
        return BinaryPrimitives.ReadUInt32BigEndian(reader.ReadBytes(4));
    }

    private static uint ReadU32BE(byte[] data, int offset)
    {
        return BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset, 4));
    }

    private static string ReadCString(byte[] data, int offset)
    {
        int end = offset;
        while (end < data.Length && data[end] != 0)
            end++;

        return Encoding.ASCII.GetString(data, offset, end - offset);
    }

    private static void RequireFile(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"File not found: {path}");
    }

    private static string RequireArg(string[] args, int index)
    {
        if (index >= args.Length)
            throw new InvalidOperationException("Missing required input path.");
        return args[index];
    }

    private static void PrintUsage()
    {
        Console.WriteLine("DiscExtract");
        Console.WriteLine("  GameCube ISO/GCM full disc extractor for Hocotate Toolkit");
        Console.WriteLine();
        Console.WriteLine("Credits");
        Console.WriteLine("  Referenced from Dolphin / DolphinTool");
        Console.WriteLine("  Author reference: jordan-woyak");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  DiscExtract.exe <input.iso|input.gcm> [output folder]");
        Console.WriteLine("  DiscExtract.exe --extract <input.iso|input.gcm> [output folder]");
        Console.WriteLine();
        Console.WriteLine("Output:");
        Console.WriteLine("  sys\\boot.bin");
        Console.WriteLine("  sys\\bi2.bin");
        Console.WriteLine("  sys\\apploader.img");
        Console.WriteLine("  sys\\main.dol");
        Console.WriteLine("  sys\\fst.bin");
        Console.WriteLine("  files\\...");
    }

    private readonly record struct FstEntry(string Name, bool IsDirectory, uint Offset, uint Size);
}
