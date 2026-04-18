using System.Buffers.Binary;
using System.Security.Cryptography;
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

    private const uint GameCubeDiscMagic = 0xC2339F3D;
    private const uint WiiDiscMagic = 0x5D1C9EA3;
    private const uint WbfsMagic = 0x53464257;

    private const int WiiPartitionTableAddress = 0x40000;
    private const int WiiPartitionGroups = 4;
    private const int WiiPartitionTicketSize = 0x2A4;
    private const int WiiPartitionTmdSizeAddress = 0x2A4;
    private const int WiiPartitionTmdOffsetAddress = 0x2A8;
    private const int WiiPartitionCertSizeAddress = 0x2AC;
    private const int WiiPartitionCertOffsetAddress = 0x2B0;
    private const int WiiPartitionH3OffsetAddress = 0x2B4;
    private const int WiiPartitionH3Size = 0x18000;
    private const int WiiPartitionDataOffsetAddress = 0x2B8;
    private const int WiiRegionAddress = 0x4E000;
    private const int WiiRegionSize = 0x20;
    private const int WiiHeaderSize = 0x100;
    private const int WiiBlockHeaderSize = 0x400;
    private const int WiiBlockDataSize = 0x7C00;
    private const int WiiBlockTotalSize = WiiBlockHeaderSize + WiiBlockDataSize;

    private static readonly Encoding DiscNameEncoding = CreateDiscNameEncoding();
    private static readonly char[] InvalidPathChars = Path.GetInvalidFileNameChars();

    private static readonly byte[] RetailCommonKey =
    {
        0xEB, 0xE4, 0x2A, 0x22, 0x5E, 0x85, 0x93, 0xE4, 0x48, 0xD9, 0xC5, 0x45, 0x73, 0x81, 0xAA, 0xF7
    };

    private static readonly byte[] KoreanCommonKey =
    {
        0x63, 0xB8, 0x2B, 0xB4, 0xF4, 0x61, 0x4E, 0x2E, 0x13, 0xF2, 0xFE, 0xFB, 0xBA, 0x4C, 0x9B, 0x7E
    };

    private static readonly byte[] DevCommonKey =
    {
        0xA1, 0x60, 0x4A, 0x6A, 0x71, 0x23, 0xB5, 0x29, 0xAE, 0x8B, 0xEC, 0x32, 0xC8, 0x16, 0xFC, 0xAA
    };

    private static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            PrintUsage();
            return 1;
        }

        try
        {
            if (!args[0].StartsWith('-') && !args[0].StartsWith('/'))
                return ExtractDisc(args[0], args.Length >= 2 ? args[1] : null);

            string mode = args[0].TrimStart('-', '/').ToLowerInvariant();
            if (mode is "extract" or "gcextract" or "wiiextract" or "wiextract" or "discextract")
                return ExtractDisc(RequireArg(args, 1), args.Length >= 3 ? args[2] : null);

            PrintUsage();
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private static int ExtractDisc(string inputPath, string? outputDir)
    {
        RequireFile(inputPath);
        outputDir ??= Path.Combine(
            Path.GetDirectoryName(inputPath) ?? ".",
            Path.GetFileNameWithoutExtension(inputPath));

        Directory.CreateDirectory(outputDir);

        using DiscImageReader disc = OpenDiscImage(inputPath);
        DiscKind discKind = DetectDiscKind(disc);

        return discKind switch
        {
            DiscKind.GameCube => ExtractGameCubeDisc(disc, inputPath, outputDir),
            DiscKind.Wii => ExtractWiiDisc(disc, inputPath, outputDir),
            _ => throw new InvalidOperationException("Supported formats: GameCube .iso/.gcm and Wii .iso/.wbfs")
        };
    }

    private static int ExtractGameCubeDisc(DiscImageReader disc, string inputPath, string outputDir)
    {
        uint dolOffset = disc.ReadUInt32BE(DolOffsetField);
        uint fstOffset = disc.ReadUInt32BE(FstOffsetField);
        uint fstSize = disc.ReadUInt32BE(FstSizeField);

        string sysDir = Path.Combine(outputDir, "sys");
        string filesDir = Path.Combine(outputDir, "files");
        Directory.CreateDirectory(sysDir);
        Directory.CreateDirectory(filesDir);

        disc.WriteRegion(0, BootBinSize, Path.Combine(sysDir, "boot.bin"));
        disc.WriteRegion(Bi2BinOffset, Bi2BinSize, Path.Combine(sysDir, "bi2.bin"));
        disc.WriteRegion(ApploaderOffset, GetApploaderSize(disc, ApploaderOffset), Path.Combine(sysDir, "apploader.img"));
        disc.WriteRegion(dolOffset, GetDolSize(disc, dolOffset), Path.Combine(sysDir, "main.dol"));
        disc.WriteRegion(fstOffset, fstSize, Path.Combine(sysDir, "fst.bin"));

        Console.WriteLine($"Extract GameCube disc: {inputPath} -> {outputDir}");
        ExtractFilesystem(new DiscPartitionReader(disc), fstOffset, fstSize, filesDir, offsetShift: 0);
        Console.WriteLine("Finished Successfully!");
        return 0;
    }

    private static int ExtractWiiDisc(DiscImageReader disc, string inputPath, string outputDir)
    {
        WiiPartitionInfo partition = FindGamePartition(disc)
            ?? throw new InvalidOperationException("No Wii DATA partition was found.");

        string sysDir = Path.Combine(outputDir, "sys");
        string filesDir = Path.Combine(outputDir, "files");
        string discDir = Path.Combine(outputDir, "disc");
        Directory.CreateDirectory(sysDir);
        Directory.CreateDirectory(filesDir);
        Directory.CreateDirectory(discDir);

        disc.WriteRegion(0, WiiHeaderSize, Path.Combine(discDir, "header.bin"));
        disc.WriteRegion(WiiRegionAddress, WiiRegionSize, Path.Combine(discDir, "region.bin"));
        disc.WriteRegion(partition.Offset, WiiPartitionTicketSize, Path.Combine(outputDir, "ticket.bin"));

        uint tmdSize = disc.ReadUInt32BE(partition.Offset + WiiPartitionTmdSizeAddress);
        long tmdOffset = partition.Offset + ((long)disc.ReadUInt32BE(partition.Offset + WiiPartitionTmdOffsetAddress) << 2);
        disc.WriteRegion(tmdOffset, tmdSize, Path.Combine(outputDir, "tmd.bin"));

        uint certSize = disc.ReadUInt32BE(partition.Offset + WiiPartitionCertSizeAddress);
        long certOffset = partition.Offset + ((long)disc.ReadUInt32BE(partition.Offset + WiiPartitionCertOffsetAddress) << 2);
        disc.WriteRegion(certOffset, certSize, Path.Combine(outputDir, "cert.bin"));

        long h3Offset = partition.Offset + ((long)disc.ReadUInt32BE(partition.Offset + WiiPartitionH3OffsetAddress) << 2);
        if (h3Offset > partition.Offset)
            disc.WriteRegion(h3Offset, WiiPartitionH3Size, Path.Combine(outputDir, "h3.bin"));

        using WiiPartitionReader partitionReader = WiiPartitionReader.Create(disc, partition);
        uint dolOffset = partitionReader.ReadUInt32Shifted(DolOffsetField);
        uint fstOffset = partitionReader.ReadUInt32Shifted(FstOffsetField);
        uint fstSize = partitionReader.ReadUInt32Shifted(FstSizeField);

        partitionReader.WriteRegion(0, BootBinSize, Path.Combine(sysDir, "boot.bin"));
        partitionReader.WriteRegion(Bi2BinOffset, Bi2BinSize, Path.Combine(sysDir, "bi2.bin"));
        partitionReader.WriteRegion(ApploaderOffset, GetApploaderSize(partitionReader, ApploaderOffset), Path.Combine(sysDir, "apploader.img"));
        partitionReader.WriteRegion(dolOffset, GetDolSize(partitionReader, dolOffset), Path.Combine(sysDir, "main.dol"));
        partitionReader.WriteRegion(fstOffset, fstSize, Path.Combine(sysDir, "fst.bin"));

        Console.WriteLine($"Extract Wii disc: {inputPath} -> {outputDir}");
        ExtractFilesystem(partitionReader, fstOffset, fstSize, filesDir, offsetShift: 2);
        Console.WriteLine("Finished Successfully!");
        return 0;
    }

    private static WiiPartitionInfo? FindGamePartition(DiscImageReader disc)
    {
        for (int group = 0; group < WiiPartitionGroups; group++)
        {
            long tableInfoOffset = WiiPartitionTableAddress + group * 8L;
            uint partitionCount = disc.ReadUInt32BE(tableInfoOffset);
            long tableOffset = (long)disc.ReadUInt32BE(tableInfoOffset + 4) << 2;

            for (int i = 0; i < partitionCount; i++)
            {
                long entryOffset = tableOffset + i * 8L;
                long partitionOffset = (long)disc.ReadUInt32BE(entryOffset) << 2;
                uint partitionType = disc.ReadUInt32BE(entryOffset + 4);
                if (partitionType == 0)
                    return new WiiPartitionInfo(partitionOffset, partitionType);
            }
        }

        return null;
    }

    private static void ExtractFilesystem(IPartitionReader partitionReader, uint fstOffset, uint fstSize, string filesDir, int offsetShift)
    {
        byte[] fst = partitionReader.ReadBytes(fstOffset, fstSize);
        FstEntry[] entries = ParseFst(fst, offsetShift);
        if (entries.Length == 0)
            throw new InvalidOperationException("FST is empty.");

        int rootEnd = checked((int)entries[0].Size);
        ExtractDirectory(partitionReader, entries, 0, 1, rootEnd, filesDir);
    }

    private static int ExtractDirectory(IPartitionReader partitionReader, FstEntry[] entries, int directoryIndex,
                                        int childIndex, int endIndex, string currentOutputDir)
    {
        Directory.CreateDirectory(currentOutputDir);
        int index = childIndex;
        while (index < endIndex)
        {
            FstEntry entry = entries[index];
            string outputPath = Path.Combine(currentOutputDir, SanitizePathSegment(entry.Name));
            if (entry.IsDirectory)
            {
                Console.WriteLine($"[DIR ] {GetDisplayPath(outputPath)}");
                index = ExtractDirectory(partitionReader, entries, index, index + 1, checked((int)entry.Size), outputPath);
            }
            else
            {
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
                Console.WriteLine($"[FILE] {GetDisplayPath(outputPath)}");
                partitionReader.WriteRegion(entry.Offset, entry.Size, outputPath);
                index++;
            }
        }

        return directoryIndex == 0 ? endIndex : index;
    }

    private static FstEntry[] ParseFst(byte[] fst, int offsetShift)
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
            uint rawOffset = ReadU32BE(fst, entryOffset + 4);
            entries[i] = new FstEntry(name, isDirectory, isDirectory ? rawOffset : rawOffset << offsetShift, ReadU32BE(fst, entryOffset + 8));
        }

        return entries;
    }

    private static uint GetApploaderSize(IReadableSource source, long apploaderOffset)
    {
        uint codeSize = source.ReadUInt32BE(apploaderOffset + 0x14);
        uint trailerSize = source.ReadUInt32BE(apploaderOffset + 0x18);
        return checked((uint)(0x20 + codeSize + trailerSize));
    }

    private static uint GetDolSize(IReadableSource source, uint dolOffset)
    {
        const int sectionCount = 18;
        const int sectionSizeTableOffset = 0x90;
        const int headerSize = 0x100;

        uint maxEnd = headerSize;
        for (int i = 0; i < sectionCount; i++)
        {
            uint sectionOffset = source.ReadUInt32BE(dolOffset + i * 4L);
            uint sectionSize = source.ReadUInt32BE(dolOffset + sectionSizeTableOffset + i * 4L);
            if (sectionOffset == 0 || sectionSize == 0)
                continue;

            uint end = checked(sectionOffset + sectionSize);
            if (end > maxEnd)
                maxEnd = end;
        }

        return maxEnd;
    }

    private static DiscKind DetectDiscKind(DiscImageReader disc)
    {
        if (disc.ReadUInt32BE(0x18) == WiiDiscMagic)
            return DiscKind.Wii;
        if (disc.ReadUInt32BE(0x1C) == GameCubeDiscMagic)
            return DiscKind.GameCube;
        return DiscKind.Unknown;
    }

    private static DiscImageReader OpenDiscImage(string inputPath)
    {
        string ext = Path.GetExtension(inputPath).ToLowerInvariant();
        return ext switch
        {
            ".wbfs" => new WbfsDiscImageReader(inputPath),
            ".iso" or ".gcm" => new PlainDiscImageReader(inputPath),
            _ => throw new InvalidOperationException("Supported formats: .iso, .gcm, .wbfs")
        };
    }

    private static uint ReadU32BE(byte[] data, int offset) => BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset, 4));

    private static string ReadCString(byte[] data, int offset)
    {
        int end = offset;
        while (end < data.Length && data[end] != 0)
            end++;
        return DiscNameEncoding.GetString(data, offset, end - offset);
    }

    private static string GetDisplayPath(string outputPath) => outputPath.Replace(Path.DirectorySeparatorChar, '/');

    private static string SanitizePathSegment(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "_";

        string sanitized = string.Join("_", name.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        sanitized = sanitized.TrimEnd('.', ' ');

        StringBuilder builder = new(sanitized.Length);
        foreach (char ch in sanitized)
            builder.Append(Array.IndexOf(InvalidPathChars, ch) >= 0 ? '_' : ch);

        return builder.Length == 0 ? "_" : builder.ToString();
    }

    private static Encoding CreateDiscNameEncoding()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return Encoding.GetEncoding(932);
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
        Console.WriteLine("  GameCube / Wii disc extractor for Hocotate Toolkit");
        Console.WriteLine();
        Console.WriteLine("Credits");
        Console.WriteLine("  This tool is part of Hocotate Toolkit by C2H4.");
        Console.WriteLine("  Implementation referenced Dolphin / DolphinTool by jordan-woyak.");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  DiscExtract.exe <input.iso|input.gcm|input.wbfs> [output folder]");
        Console.WriteLine("  DiscExtract.exe --extract <input.iso|input.gcm|input.wbfs> [output folder]");
        Console.WriteLine("  DiscExtract.exe --gcextract <input.iso|input.gcm> [output folder]");
        Console.WriteLine("  DiscExtract.exe --wiiextract <input.iso|input.wbfs> [output folder]");
    }

    private enum DiscKind { Unknown, GameCube, Wii }
    private readonly record struct FstEntry(string Name, bool IsDirectory, uint Offset, uint Size);
    private readonly record struct WiiPartitionInfo(long Offset, uint Type);

    private interface IReadableSource
    {
        uint ReadUInt32BE(long offset);
    }

    private interface IPartitionReader : IReadableSource
    {
        byte[] ReadBytes(uint offset, uint size);
        void WriteRegion(uint offset, uint size, string outputPath);
    }

    private abstract class DiscImageReader : IReadableSource, IDisposable
    {
        public abstract uint ReadUInt32BE(long offset);
        public abstract void Read(long offset, Span<byte> buffer);
        public abstract void Dispose();

        public byte[] ReadBytes(long offset, long size)
        {
            byte[] buffer = new byte[checked((int)size)];
            Read(offset, buffer);
            return buffer;
        }

        public void WriteRegion(long offset, long size, string outputPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            using FileStream output = new(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
            byte[] buffer = new byte[1024 * 1024];
            long remaining = size;
            long position = offset;
            while (remaining > 0)
            {
                int chunk = (int)Math.Min(buffer.Length, remaining);
                Read(position, buffer.AsSpan(0, chunk));
                output.Write(buffer, 0, chunk);
                position += chunk;
                remaining -= chunk;
            }
        }
    }

    private sealed class PlainDiscImageReader : DiscImageReader
    {
        private readonly FileStream _stream;

        public PlainDiscImageReader(string path)
        {
            _stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        public override uint ReadUInt32BE(long offset)
        {
            Span<byte> buffer = stackalloc byte[4];
            Read(offset, buffer);
            return BinaryPrimitives.ReadUInt32BigEndian(buffer);
        }

        public override void Read(long offset, Span<byte> buffer)
        {
            _stream.Position = offset;
            int readTotal = 0;
            while (readTotal < buffer.Length)
            {
                int read = _stream.Read(buffer[readTotal..]);
                if (read == 0)
                    throw new EndOfStreamException("Unexpected end of file.");
                readTotal += read;
            }
        }

        public override void Dispose() => _stream.Dispose();
    }

    private sealed class WbfsDiscImageReader : DiscImageReader
    {
        private const long WiiSectorSize = 0x8000;
        private const long WiiSectorCount = 143432L * 2;
        private const long WiiDiscHeaderSize = 0x100;

        private readonly List<WbfsSegment> _segments = new();
        private readonly ushort[] _wlbaTable;
        private readonly long _wbfsSectorSize;
        private readonly long _blocksPerDisc;

        public WbfsDiscImageReader(string path)
        {
            AddSegment(path);
            OpenAdditionalSegments(path);

            byte[] header = ReadPhysicalBytes(0, 0x200);
            if (BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(0, 4)) != WbfsMagic)
                throw new InvalidOperationException("Invalid WBFS header.");

            uint hdSectorCount = BinaryPrimitives.ReadUInt32BigEndian(header.AsSpan(4, 4));
            int hdSectorShift = header[8];
            int wbfsSectorShift = header[9];
            long hdSectorSize = 1L << hdSectorShift;
            _wbfsSectorSize = 1L << wbfsSectorShift;

            long totalPhysicalSize = _segments.Sum(s => s.Size);
            if (totalPhysicalSize != hdSectorCount * hdSectorSize)
                throw new InvalidOperationException("WBFS size does not match its header.");
            if (_wbfsSectorSize < WiiSectorSize)
                throw new InvalidOperationException("WBFS sector size is too small.");
            if (header[12] == 0)
                throw new InvalidOperationException("WBFS does not contain a disc in slot 0.");

            _blocksPerDisc = (WiiSectorCount * WiiSectorSize + _wbfsSectorSize - 1) / _wbfsSectorSize;
            int wlbaTableSize = checked((int)(_blocksPerDisc * sizeof(ushort)));
            byte[] wlbaBytes = ReadPhysicalBytes(hdSectorSize + WiiDiscHeaderSize, wlbaTableSize);
            _wlbaTable = new ushort[_blocksPerDisc];
            for (int i = 0; i < _wlbaTable.Length; i++)
                _wlbaTable[i] = BinaryPrimitives.ReadUInt16BigEndian(wlbaBytes.AsSpan(i * 2, 2));
        }

        public override uint ReadUInt32BE(long offset)
        {
            Span<byte> buffer = stackalloc byte[4];
            Read(offset, buffer);
            return BinaryPrimitives.ReadUInt32BigEndian(buffer);
        }

        public override void Read(long offset, Span<byte> buffer)
        {
            if (offset < 0 || offset + buffer.Length > WiiSectorCount * WiiSectorSize)
                throw new EndOfStreamException("Read beyond the virtual WBFS disc size.");

            int written = 0;
            long currentOffset = offset;
            while (written < buffer.Length)
            {
                long blockIndex = currentOffset / _wbfsSectorSize;
                if (blockIndex >= _blocksPerDisc)
                    throw new EndOfStreamException("Read beyond the WBFS block table.");

                ushort block = _wlbaTable[blockIndex];
                if (block == 0)
                    throw new InvalidOperationException("Encountered an unmapped WBFS block while reading disc data.");

                long clusterOffset = currentOffset & (_wbfsSectorSize - 1);
                int chunk = (int)Math.Min(buffer.Length - written, _wbfsSectorSize - clusterOffset);
                ReadPhysical(block * _wbfsSectorSize + clusterOffset, buffer.Slice(written, chunk));
                currentOffset += chunk;
                written += chunk;
            }
        }

        public override void Dispose()
        {
            foreach (WbfsSegment segment in _segments)
                segment.Stream.Dispose();
        }

        private void OpenAdditionalSegments(string path)
        {
            if (!path.EndsWith(".wbfs", StringComparison.OrdinalIgnoreCase))
                return;

            for (int i = 1; i < 10; i++)
            {
                string sibling = Path.ChangeExtension(path, $"wbf{i}");
                if (!File.Exists(sibling))
                    break;
                AddSegment(sibling);
            }
        }

        private void AddSegment(string path)
        {
            FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            long baseOffset = _segments.Count == 0 ? 0 : _segments[^1].BaseOffset + _segments[^1].Size;
            _segments.Add(new WbfsSegment(stream, baseOffset));
        }

        private byte[] ReadPhysicalBytes(long offset, int size)
        {
            byte[] buffer = new byte[size];
            ReadPhysical(offset, buffer);
            return buffer;
        }

        private void ReadPhysical(long offset, Span<byte> buffer)
        {
            int readTotal = 0;
            long currentOffset = offset;
            while (readTotal < buffer.Length)
            {
                WbfsSegment segment = FindSegment(currentOffset);
                long localOffset = currentOffset - segment.BaseOffset;
                int chunk = (int)Math.Min(buffer.Length - readTotal, segment.Size - localOffset);
                segment.Stream.Position = localOffset;

                int read = 0;
                while (read < chunk)
                {
                    int current = segment.Stream.Read(buffer.Slice(readTotal + read, chunk - read));
                    if (current == 0)
                        throw new EndOfStreamException("Unexpected end of WBFS segment.");
                    read += current;
                }

                currentOffset += chunk;
                readTotal += chunk;
            }
        }

        private WbfsSegment FindSegment(long offset)
        {
            foreach (WbfsSegment segment in _segments)
            {
                if (offset >= segment.BaseOffset && offset < segment.BaseOffset + segment.Size)
                    return segment;
            }

            throw new EndOfStreamException("WBFS physical offset is out of range.");
        }

        private sealed class WbfsSegment
        {
            public WbfsSegment(FileStream stream, long baseOffset)
            {
                Stream = stream;
                BaseOffset = baseOffset;
                Size = stream.Length;
            }

            public FileStream Stream { get; }
            public long BaseOffset { get; }
            public long Size { get; }
        }
    }

    private sealed class DiscPartitionReader : IPartitionReader
    {
        private readonly DiscImageReader _disc;

        public DiscPartitionReader(DiscImageReader disc)
        {
            _disc = disc;
        }

        public uint ReadUInt32BE(long offset) => _disc.ReadUInt32BE(offset);
        public byte[] ReadBytes(uint offset, uint size) => _disc.ReadBytes(offset, size);
        public void WriteRegion(uint offset, uint size, string outputPath) => _disc.WriteRegion(offset, size, outputPath);
    }

    private sealed class WiiPartitionReader : IPartitionReader, IDisposable
    {
        private const int TicketTitleKeyOffset = 0x1BF;
        private const int TicketTitleIdOffset = 0x1DC;
        private const int TicketCommonKeyIndexOffset = 0x1F1;
        private const int SignatureIssuerOffset = 0x140;
        private const int SignatureIssuerLength = 0x40;

        private readonly DiscImageReader _disc;
        private readonly long _partitionOffset;
        private readonly long _partitionDataOffset;
        private readonly bool _hasHashes;
        private readonly bool _hasEncryption;
        private readonly byte[] _titleKey;
        private readonly byte[] _encryptedBlock = new byte[WiiBlockTotalSize];
        private readonly byte[] _decryptedBlock = new byte[WiiBlockDataSize];
        private long _cachedBlockOffset = -1;

        private WiiPartitionReader(DiscImageReader disc, long partitionOffset, long partitionDataOffset,
                                   bool hasHashes, bool hasEncryption, byte[] titleKey)
        {
            _disc = disc;
            _partitionOffset = partitionOffset;
            _partitionDataOffset = partitionDataOffset;
            _hasHashes = hasHashes;
            _hasEncryption = hasEncryption;
            _titleKey = titleKey;
        }

        public static WiiPartitionReader Create(DiscImageReader disc, WiiPartitionInfo partition)
        {
            byte[] ticket = disc.ReadBytes(partition.Offset, WiiPartitionTicketSize);
            byte[] titleKey = DecryptTitleKey(ticket);
            long partitionDataOffset = (long)disc.ReadUInt32BE(partition.Offset + WiiPartitionDataOffsetAddress) << 2;
            bool hasHashes = disc.ReadBytes(0x60, 1)[0] == 0;
            bool hasEncryption = disc.ReadBytes(0x61, 1)[0] == 0;
            return new WiiPartitionReader(disc, partition.Offset, partitionDataOffset, hasHashes, hasEncryption, titleKey);
        }

        public uint ReadUInt32BE(long offset)
        {
            Span<byte> buffer = stackalloc byte[4];
            Read(offset, buffer);
            return BinaryPrimitives.ReadUInt32BigEndian(buffer);
        }

        public uint ReadUInt32Shifted(long offset) => ReadUInt32BE(offset) << 2;

        public byte[] ReadBytes(uint offset, uint size)
        {
            byte[] buffer = new byte[checked((int)size)];
            Read(offset, buffer);
            return buffer;
        }

        public void WriteRegion(uint offset, uint size, string outputPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            using FileStream output = new(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);

            byte[] buffer = new byte[1024 * 1024];
            uint remaining = size;
            uint position = offset;
            while (remaining > 0)
            {
                int chunk = (int)Math.Min((uint)buffer.Length, remaining);
                Read(position, buffer.AsSpan(0, chunk));
                output.Write(buffer, 0, chunk);
                position += (uint)chunk;
                remaining -= (uint)chunk;
            }
        }

        public void Dispose()
        {
        }

        private void Read(long offset, Span<byte> buffer)
        {
            if (!_hasHashes)
            {
                _disc.Read(_partitionOffset + _partitionDataOffset + offset, buffer);
                return;
            }

            int written = 0;
            long currentOffset = offset;
            while (written < buffer.Length)
            {
                long blockOffsetOnDisc = _partitionOffset + _partitionDataOffset + (currentOffset / WiiBlockDataSize) * WiiBlockTotalSize;
                int dataOffsetInBlock = (int)(currentOffset % WiiBlockDataSize);
                EnsureBlockLoaded(blockOffsetOnDisc);

                int copySize = Math.Min(buffer.Length - written, WiiBlockDataSize - dataOffsetInBlock);
                _decryptedBlock.AsSpan(dataOffsetInBlock, copySize).CopyTo(buffer.Slice(written, copySize));
                written += copySize;
                currentOffset += copySize;
            }
        }

        private void EnsureBlockLoaded(long blockOffsetOnDisc)
        {
            if (_cachedBlockOffset == blockOffsetOnDisc)
                return;

            _disc.Read(blockOffsetOnDisc, _encryptedBlock);

            if (_hasEncryption)
            {
                byte[] iv = _encryptedBlock.AsSpan(0x3D0, 16).ToArray();
                using Aes aes = Aes.Create();
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.None;
                aes.Key = _titleKey;
                aes.IV = iv;
                using ICryptoTransform decryptor = aes.CreateDecryptor();
                decryptor.TransformBlock(_encryptedBlock, WiiBlockHeaderSize, WiiBlockDataSize, _decryptedBlock, 0);
            }
            else
            {
                Buffer.BlockCopy(_encryptedBlock, WiiBlockHeaderSize, _decryptedBlock, 0, WiiBlockDataSize);
            }

            _cachedBlockOffset = blockOffsetOnDisc;
        }

        private static byte[] DecryptTitleKey(byte[] ticket)
        {
            byte[] iv = new byte[16];
            Buffer.BlockCopy(ticket, TicketTitleIdOffset, iv, 0, 8);

            using Aes aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.None;
            aes.Key = GetCommonKey(ticket, ticket[TicketCommonKeyIndexOffset]);
            aes.IV = iv;
            using ICryptoTransform decryptor = aes.CreateDecryptor();
            return decryptor.TransformFinalBlock(ticket, TicketTitleKeyOffset, 16);
        }

        private static byte[] GetCommonKey(byte[] ticket, byte keyIndex)
        {
            return keyIndex switch
            {
                0 when IsDevTicket(ticket) => DevCommonKey,
                0 => RetailCommonKey,
                1 => KoreanCommonKey,
                _ => throw new InvalidOperationException($"Unsupported Wii common key index: {keyIndex}")
            };
        }

        private static bool IsDevTicket(byte[] ticket)
        {
            string issuer = Encoding.ASCII.GetString(ticket, SignatureIssuerOffset, SignatureIssuerLength).TrimEnd('\0');
            return issuer == "Root-CA00000002-XS00000006";
        }
    }
}
