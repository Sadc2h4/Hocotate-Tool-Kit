using System.Buffers.Binary;
using System.Security.Cryptography;
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

    private const int GcFileAlignment = 0x20;
    private const int WiiFileAlignment = 0x8000;

    private const int WiiPartitionTableAddress = 0x40000;
    private const int WiiPartitionSubtable1Offset = 0x20;
    private const long WiiStandardGamePartitionAddress = 0x0F800000;
    private const long WiiPartitionDataOffset = 0x20000;
    private const int WiiTmdOffset = 0x2C0;
    private const int WiiH3Offset = 0x4000;
    private const int WiiPartitionTicketSize = 0x2A4;
    private const int WiiPartitionHeaderSize = 0x1C;
    private const int WiiRegionAddress = 0x4E000;
    private const int WiiRegionSize = 0x20;
    private const int WiiHeaderSize = 0x100;
    private const int WiiBlockHeaderSize = 0x400;
    private const int WiiBlockDataSize = 0x7C00;
    private const int WiiBlockTotalSize = 0x8000;
    private const int WiiBlocksPerGroup = 0x40;
    private const int WiiGroupDataSize = WiiBlockDataSize * WiiBlocksPerGroup;
    private const int WiiGroupTotalSize = WiiBlockTotalSize * WiiBlocksPerGroup;
    private const int WiiH3Size = 0x18000;

    private const uint WbfsMagic = 0x53464257;
    private const int WbfsHdSectorShift = 9;
    private const int WbfsSectorShift = 21;
    private const long WbfsWiiSectorSize = 0x8000;
    private const long WbfsWiiSectorCount = 143432L * 2;
    private const long WbfsDiscHeaderSize = 0x100;

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

        if (!args[0].StartsWith('-') && !args[0].StartsWith('/'))
            return RebuildDisc(args[0], args.Length >= 2 ? args[1] : null);

        string mode = args[0].TrimStart('-', '/').ToLowerInvariant();
        if (mode is "rebuild" or "gcrebuild" or "wiirebuild" or "wirebuild")
            return RebuildDisc(args[1], args.Length >= 3 ? args[2] : null);
        if (mode is "iso2wbfs" or "isotowbfs" or "wbfsconvert")
            return ConvertIsoToWbfs(RequireArg(args, 1), args.Length >= 3 ? args[2] : null);

        PrintUsage();
        return 1;
    }

    private static int RebuildDisc(string inputFolder, string? outputPath)
    {
        try
        {
            DiscKind kind = DetectDiscKind(inputFolder);
            return kind switch
            {
                DiscKind.GameCube => RebuildGameCubeDisc(inputFolder, outputPath),
                DiscKind.Wii => RebuildWiiDisc(inputFolder, outputPath),
                _ => throw new InvalidOperationException("Input folder is not a supported GameCube/Wii disc root.")
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private static int ConvertIsoToWbfs(string inputIso, string? outputPath)
    {
        try
        {
            RequireFile(inputIso);
            outputPath ??= Path.Combine(
                Path.GetDirectoryName(inputIso) ?? ".",
                Path.GetFileNameWithoutExtension(inputIso) + ".wbfs");

            WriteWbfsFromIso(inputIso, outputPath);
            Console.WriteLine($"Convert ISO to WBFS: {inputIso} -> {outputPath}");
            Console.WriteLine("Finished Successfully!");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private static int RebuildGameCubeDisc(string inputFolder, string? outputPath)
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

        outputPath ??= DefaultOutputPath(inputFolder, ".iso");

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

        uint dolOffset = Align((uint)(ApploaderOffset + apploader.Length), GcFileAlignment);
        uint fstOffset = Align((uint)(dolOffset + mainDol.Length), GcFileAlignment);
        byte[] fst = BuildFst(entries, stringTable, addressShift: 0);
        uint fstSize = (uint)fst.Length;
        uint dataOffset = Align(fstOffset + fstSize, GcFileAlignment);
        AssignFileOffsets(entries, dataOffset, GcFileAlignment);
        fst = BuildFst(entries, stringTable, addressShift: 0);
        fstSize = (uint)fst.Length;
        dataOffset = Align(fstOffset + fstSize, GcFileAlignment);
        AssignFileOffsets(entries, dataOffset, GcFileAlignment);
        fst = BuildFst(entries, stringTable, addressShift: 0);
        fstSize = (uint)fst.Length;

        uint finalSize = dataOffset;
        foreach (FstEntryData entry in entries.Where(e => !e.IsDirectory))
            finalSize = Align(entry.FileOffset + (uint)entry.FileSize, GcFileAlignment);

        PatchGameCubeBootBin(bootBin, dolOffset, fstOffset, fstSize, dataOffset, finalSize - dataOffset);

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
        Console.WriteLine($"Rebuild GameCube disc: {inputFolder} -> {outputPath}");
        Console.WriteLine("Finished Successfully!");
        return 0;
    }

    private static int RebuildWiiDisc(string inputFolder, string? outputPath)
    {
        string sysDir = Path.Combine(inputFolder, "sys");
        string filesDir = Path.Combine(inputFolder, "files");
        string discDir = Path.Combine(inputFolder, "disc");

        RequireDirectory(inputFolder);
        RequireDirectory(sysDir);
        RequireDirectory(filesDir);
        RequireDirectory(discDir);

        string bootPath = Path.Combine(sysDir, "boot.bin");
        string bi2Path = Path.Combine(sysDir, "bi2.bin");
        string apploaderPath = Path.Combine(sysDir, "apploader.img");
        string dolPath = Path.Combine(sysDir, "main.dol");
        string headerPath = Path.Combine(discDir, "header.bin");
        string regionPath = Path.Combine(discDir, "region.bin");
        string ticketPath = Path.Combine(inputFolder, "ticket.bin");
        string tmdPath = Path.Combine(inputFolder, "tmd.bin");
        string certPath = Path.Combine(inputFolder, "cert.bin");

        RequireFile(bootPath);
        RequireFile(bi2Path);
        RequireFile(apploaderPath);
        RequireFile(dolPath);
        RequireFile(headerPath);
        RequireFile(regionPath);
        RequireFile(ticketPath);
        RequireFile(tmdPath);
        RequireFile(certPath);

        outputPath ??= DefaultOutputPath(inputFolder, ".wbfs");

        byte[] bootBin = File.ReadAllBytes(bootPath);
        byte[] bi2Bin = File.ReadAllBytes(bi2Path);
        byte[] apploader = File.ReadAllBytes(apploaderPath);
        byte[] mainDol = File.ReadAllBytes(dolPath);
        byte[] headerBin = File.ReadAllBytes(headerPath);
        byte[] regionBin = File.ReadAllBytes(regionPath);
        byte[] ticketBin = File.ReadAllBytes(ticketPath);
        byte[] tmdBin = File.ReadAllBytes(tmdPath);
        byte[] certBin = File.ReadAllBytes(certPath);

        ValidateLength(bootBin, BootBinSize, "sys\\boot.bin");
        ValidateLength(bi2Bin, Bi2BinSize, "sys\\bi2.bin");
        ValidateLength(headerBin, WiiHeaderSize, "disc\\header.bin");
        ValidateLength(regionBin, WiiRegionSize, "disc\\region.bin");

        DirectoryNode root = BuildTree(filesDir);
        List<FstEntryData> entries = new();
        List<byte> stringTable = new();
        AddDirectoryEntries(root, parentIndex: 0, entries, stringTable);

        uint dolOffset = Align((uint)(ApploaderOffset + apploader.Length + 0x20), GcFileAlignment);
        uint fstOffset = Align((uint)(dolOffset + mainDol.Length + 0x20), GcFileAlignment);
        byte[] fst = BuildFst(entries, stringTable, addressShift: 2);
        uint fstSize = (uint)fst.Length;
        uint dataOffset = Align(fstOffset + fstSize, WiiFileAlignment);
        AssignFileOffsets(entries, dataOffset, WiiFileAlignment);
        fst = BuildFst(entries, stringTable, addressShift: 2);
        fstSize = (uint)fst.Length;
        dataOffset = Align(fstOffset + fstSize, WiiFileAlignment);
        AssignFileOffsets(entries, dataOffset, WiiFileAlignment);
        fst = BuildFst(entries, stringTable, addressShift: 2);
        fstSize = (uint)fst.Length;

        PatchWiiBootBin(bootBin, dolOffset, fstOffset, fstSize);

        List<ContentEntry> contents =
        [
            new ContentEntry(0, bootBin),
            new ContentEntry(BootBinSize, bi2Bin),
            new ContentEntry(ApploaderOffset, apploader),
            new ContentEntry(dolOffset, mainDol),
            new ContentEntry(fstOffset, fst)
        ];

        foreach (FstEntryData entry in entries.Where(e => !e.IsDirectory))
            contents.Add(new ContentEntry(entry.FileOffset, entry.FileSize, entry.SourcePath!, entry.Path));

        long decryptedEnd = dataOffset;
        foreach (FstEntryData entry in entries.Where(e => !e.IsDirectory))
            decryptedEnd = Math.Max(decryptedEnd, AlignLong(entry.FileOffset + entry.FileSize, WiiFileAlignment));

        long partitionDataSizeAligned = AlignLong(decryptedEnd, WiiBlockDataSize);
        long encryptedPartitionDataSize = partitionDataSizeAligned / WiiBlockDataSize * WiiBlockTotalSize;
        long partitionAddress = WiiStandardGamePartitionAddress;
        long partitionDataAddress = partitionAddress + WiiPartitionDataOffset;
        long nextPartitionAddress = AlignLong(partitionDataAddress + encryptedPartitionDataSize, 0x10000);

        byte[] titleKey = GetTitleKeyFromTicket(ticketBin);
        byte[] partitionHeader = BuildWiiPartitionHeader(tmdBin.Length, certBin.Length, partitionDataSizeAligned);
        byte[] h3Bin = BuildWiiH3Table(contents, partitionDataSizeAligned);

        string finalExtension = Path.GetExtension(outputPath).ToLowerInvariant();
        string isoPath = finalExtension == ".wbfs"
            ? Path.Combine(Path.GetDirectoryName(outputPath) ?? ".", Path.GetFileNameWithoutExtension(outputPath) + ".tmp.iso")
            : outputPath;

        using (FileStream output = new(isoPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            WriteBlock(output, 0, headerBin);
            WriteBlock(output, WiiRegionAddress, regionBin);
            WriteBlock(output, WiiPartitionTableAddress, BuildWiiPartitionTable(partitionAddress));
            WriteBlock(output, partitionAddress, ticketBin);
            WriteBlock(output, partitionAddress + WiiPartitionTicketSize, partitionHeader);
            WriteBlock(output, partitionAddress + WiiTmdOffset, tmdBin);
            long certOffset = partitionAddress + AlignLong(WiiTmdOffset + tmdBin.Length, GcFileAlignment);
            WriteBlock(output, certOffset, certBin);
            WriteBlock(output, partitionAddress + WiiH3Offset, h3Bin);
            WriteWiiEncryptedPartition(output, partitionDataAddress, contents, partitionDataSizeAligned, titleKey);
            output.SetLength(nextPartitionAddress);
        }

        if (finalExtension == ".wbfs")
        {
            WriteWbfsFromIso(isoPath, outputPath);
            File.Delete(isoPath);
        }

        Console.WriteLine($"Rebuild Wii disc: {inputFolder} -> {outputPath}");
        Console.WriteLine("Finished Successfully!");
        return 0;
    }

    private static DiscKind DetectDiscKind(string inputFolder)
    {
        string sysDir = Path.Combine(inputFolder, "sys");
        string filesDir = Path.Combine(inputFolder, "files");
        if (!Directory.Exists(sysDir) || !Directory.Exists(filesDir))
            return DiscKind.Unknown;

        if (File.Exists(Path.Combine(inputFolder, "ticket.bin")) &&
            File.Exists(Path.Combine(inputFolder, "tmd.bin")) &&
            File.Exists(Path.Combine(inputFolder, "cert.bin")) &&
            Directory.Exists(Path.Combine(inputFolder, "disc")))
        {
            return DiscKind.Wii;
        }

        return DiscKind.GameCube;
    }

    private static void PatchGameCubeBootBin(byte[] bootBin, uint dolOffset, uint fstOffset, uint fstSize,
                                             uint userPosition, uint userLength)
    {
        WriteU32BE(bootBin, DolOffsetField, dolOffset);
        WriteU32BE(bootBin, FstOffsetField, fstOffset);
        WriteU32BE(bootBin, FstSizeField, fstSize);
        WriteU32BE(bootBin, FstMaxSizeField, fstSize);
        WriteU32BE(bootBin, UserPositionField, userPosition);
        WriteU32BE(bootBin, UserLengthField, userLength);
    }

    private static void PatchWiiBootBin(byte[] bootBin, uint dolOffset, uint fstOffset, uint fstSize)
    {
        WriteU32BE(bootBin, DolOffsetField, dolOffset >> 2);
        WriteU32BE(bootBin, FstOffsetField, fstOffset >> 2);
        WriteU32BE(bootBin, FstSizeField, fstSize >> 2);
        WriteU32BE(bootBin, FstMaxSizeField, fstSize >> 2);
    }

    private static byte[] BuildWiiPartitionTable(long partitionAddress)
    {
        byte[] table = new byte[WiiPartitionSubtable1Offset + 8];
        WriteU32BE(table, 0x0, 1);
        WriteU32BE(table, 0x4, (WiiPartitionTableAddress + WiiPartitionSubtable1Offset) >> 2);
        WriteU32BE(table, WiiPartitionSubtable1Offset, (uint)(partitionAddress >> 2));
        WriteU32BE(table, WiiPartitionSubtable1Offset + 4, 0);
        return table;
    }

    private static byte[] BuildWiiPartitionHeader(int tmdSize, int certSize, long decryptedDataSize)
    {
        byte[] header = new byte[WiiPartitionHeaderSize];
        int certOffset = Align(WiiTmdOffset + tmdSize, GcFileAlignment);
        long rawDataSize = AlignLong(decryptedDataSize, WiiBlockDataSize) / WiiBlockDataSize * WiiBlockTotalSize;

        WriteU32BE(header, 0x0, (uint)tmdSize);
        WriteU32BE(header, 0x4, WiiTmdOffset >> 2);
        WriteU32BE(header, 0x8, (uint)certSize);
        WriteU32BE(header, 0x0C, (uint)(certOffset >> 2));
        WriteU32BE(header, 0x10, WiiH3Offset >> 2);
        WriteU32BE(header, 0x14, (uint)(WiiPartitionDataOffset >> 2));
        WriteU32BE(header, 0x18, (uint)(rawDataSize >> 2));
        return header;
    }

    private static byte[] BuildWiiH3Table(List<ContentEntry> contents, long partitionDataSize)
    {
        byte[] h3 = new byte[WiiH3Size];
        using SHA1 sha1 = SHA1.Create();
        byte[] groupPlain = new byte[WiiGroupDataSize];

        int groupIndex = 0;
        for (long offset = 0; offset < partitionDataSize; offset += WiiGroupDataSize, groupIndex++)
        {
            Array.Clear(groupPlain, 0, groupPlain.Length);
            ReadPartitionData(contents, offset, groupPlain);
            byte[] h2 = BuildH2Hashes(groupPlain, sha1);
            byte[] digest = sha1.ComputeHash(h2);
            Buffer.BlockCopy(digest, 0, h3, groupIndex * 20, 20);
        }

        return h3;
    }

    private static byte[] BuildH2Hashes(byte[] groupPlain, SHA1 sha1)
    {
        byte[] h2 = new byte[8 * 20];
        for (int subgroup = 0; subgroup < 8; subgroup++)
        {
            byte[] h1 = new byte[8 * 20];
            for (int blockInSubgroup = 0; blockInSubgroup < 8; blockInSubgroup++)
            {
                int blockIndex = subgroup * 8 + blockInSubgroup;
                int blockOffset = blockIndex * WiiBlockDataSize;
                byte[] h0 = new byte[31 * 20];
                for (int chunk = 0; chunk < 31; chunk++)
                {
                    byte[] digest = sha1.ComputeHash(groupPlain, blockOffset + chunk * 0x400, 0x400);
                    Buffer.BlockCopy(digest, 0, h0, chunk * 20, 20);
                }

                byte[] h1Digest = sha1.ComputeHash(h0);
                Buffer.BlockCopy(h1Digest, 0, h1, blockInSubgroup * 20, 20);
            }

            byte[] h2Digest = sha1.ComputeHash(h1);
            Buffer.BlockCopy(h2Digest, 0, h2, subgroup * 20, 20);
        }

        return h2;
    }

    private static void WriteWiiEncryptedPartition(FileStream output, long partitionDataAddress, List<ContentEntry> contents,
                                                   long partitionDataSize, byte[] titleKey)
    {
        using SHA1 sha1 = SHA1.Create();
        byte[] groupPlain = new byte[WiiGroupDataSize];
        byte[] groupEncrypted = new byte[WiiGroupTotalSize];

        for (long offset = 0; offset < partitionDataSize; offset += WiiGroupDataSize)
        {
            Array.Clear(groupPlain, 0, groupPlain.Length);
            ReadPartitionData(contents, offset, groupPlain);
            EncryptGroup(groupPlain, groupEncrypted, titleKey, sha1);
            output.Position = partitionDataAddress + offset / WiiBlockDataSize * WiiBlockTotalSize;
            output.Write(groupEncrypted, 0, groupEncrypted.Length);
        }
    }

    private static void EncryptGroup(byte[] groupPlain, byte[] groupEncrypted, byte[] titleKey, SHA1 sha1)
    {
        byte[] allH2 = new byte[8 * 20];
        byte[][] headers = new byte[WiiBlocksPerGroup][];

        for (int subgroup = 0; subgroup < 8; subgroup++)
        {
            byte[] subgroupH1 = new byte[8 * 20];

            for (int blockInSubgroup = 0; blockInSubgroup < 8; blockInSubgroup++)
            {
                int blockIndex = subgroup * 8 + blockInSubgroup;
                int blockOffset = blockIndex * WiiBlockDataSize;
                byte[] h0 = new byte[31 * 20];
                for (int chunk = 0; chunk < 31; chunk++)
                {
                    byte[] digest = sha1.ComputeHash(groupPlain, blockOffset + chunk * 0x400, 0x400);
                    Buffer.BlockCopy(digest, 0, h0, chunk * 20, 20);
                }

                byte[] h1Digest = sha1.ComputeHash(h0);
                Buffer.BlockCopy(h1Digest, 0, subgroupH1, blockInSubgroup * 20, 20);

                byte[] header = new byte[WiiBlockHeaderSize];
                Buffer.BlockCopy(h0, 0, header, 0, h0.Length);
                headers[blockIndex] = header;
            }

            byte[] h2Digest = sha1.ComputeHash(subgroupH1);
            Buffer.BlockCopy(h2Digest, 0, allH2, subgroup * 20, 20);

            for (int blockInSubgroup = 0; blockInSubgroup < 8; blockInSubgroup++)
            {
                int blockIndex = subgroup * 8 + blockInSubgroup;
                Buffer.BlockCopy(subgroupH1, 0, headers[blockIndex], 0x280, subgroupH1.Length);
            }
        }

        for (int blockIndex = 0; blockIndex < WiiBlocksPerGroup; blockIndex++)
        {
            Buffer.BlockCopy(allH2, 0, headers[blockIndex], 0x340, allH2.Length);
            EncryptWiiBlock(groupPlain, blockIndex * WiiBlockDataSize, titleKey, headers[blockIndex],
                groupEncrypted, blockIndex * WiiBlockTotalSize);
        }
    }

    private static void EncryptWiiBlock(byte[] groupPlain, int plainOffset, byte[] titleKey, byte[] header,
                                        byte[] groupEncrypted, int encryptedOffset)
    {
        using Aes headerAes = Aes.Create();
        headerAes.Mode = CipherMode.CBC;
        headerAes.Padding = PaddingMode.None;
        headerAes.Key = titleKey;
        headerAes.IV = new byte[16];
        using ICryptoTransform headerEncryptor = headerAes.CreateEncryptor();
        headerEncryptor.TransformBlock(header, 0, WiiBlockHeaderSize, groupEncrypted, encryptedOffset);

        byte[] iv = new byte[16];
        Buffer.BlockCopy(groupEncrypted, encryptedOffset + 0x3D0, iv, 0, 16);
        using Aes dataAes = Aes.Create();
        dataAes.Mode = CipherMode.CBC;
        dataAes.Padding = PaddingMode.None;
        dataAes.Key = titleKey;
        dataAes.IV = iv;
        using ICryptoTransform dataEncryptor = dataAes.CreateEncryptor();
        dataEncryptor.TransformBlock(groupPlain, plainOffset, WiiBlockDataSize, groupEncrypted, encryptedOffset + WiiBlockHeaderSize);
    }

    private static void ReadPartitionData(List<ContentEntry> contents, long offset, byte[] destination)
    {
        long end = offset + destination.Length;
        foreach (ContentEntry content in contents)
        {
            long contentEnd = content.Offset + content.Size;
            if (contentEnd <= offset || content.Offset >= end)
                continue;

            int destOffset = (int)Math.Max(0, content.Offset - offset);
            long sourceOffset = Math.Max(0, offset - content.Offset);
            int copySize = (int)Math.Min(content.Size - sourceOffset, destination.Length - destOffset);
            content.CopyTo(sourceOffset, destination.AsSpan(destOffset, copySize));
        }
    }

    private static byte[] GetTitleKeyFromTicket(byte[] ticket)
    {
        const int titleKeyOffset = 0x1BF;
        const int titleIdOffset = 0x1DC;
        const int keyIndexOffset = 0x1F1;
        const int issuerOffset = 0x140;
        const int issuerLength = 0x40;

        byte[] iv = new byte[16];
        Buffer.BlockCopy(ticket, titleIdOffset, iv, 0, 8);
        byte[] commonKey = ticket[keyIndexOffset] switch
        {
            0 when Encoding.ASCII.GetString(ticket, issuerOffset, issuerLength).TrimEnd('\0') == "Root-CA00000002-XS00000006" => DevCommonKey,
            0 => RetailCommonKey,
            1 => KoreanCommonKey,
            _ => throw new InvalidOperationException($"Unsupported Wii common key index: {ticket[keyIndexOffset]}")
        };

        using Aes aes = Aes.Create();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.None;
        aes.Key = commonKey;
        aes.IV = iv;
        using ICryptoTransform decryptor = aes.CreateDecryptor();
        return decryptor.TransformFinalBlock(ticket, titleKeyOffset, 16);
    }

    private static void WriteWbfsFromIso(string isoPath, string wbfsPath)
    {
        const int hdSectorSize = 1 << WbfsHdSectorShift;
        const int wbfsSectorSize = 1 << WbfsSectorShift;
        int blocksPerDisc = (int)((WbfsWiiSectorCount * WbfsWiiSectorSize + wbfsSectorSize - 1) / wbfsSectorSize);
        int discInfoSize = Align((int)(WbfsDiscHeaderSize + blocksPerDisc * 2), hdSectorSize);

        using FileStream iso = new(isoPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using FileStream wbfs = new(wbfsPath, FileMode.Create, FileAccess.Write, FileShare.None);

        byte[] header = new byte[hdSectorSize];
        byte[] discInfo = new byte[discInfoSize];
        long dataStart = AlignLong(hdSectorSize + discInfoSize, wbfsSectorSize);
        ushort currentWbfsBlock = checked((ushort)(dataStart / wbfsSectorSize));
        int isoBlockCount = checked((int)((iso.Length + wbfsSectorSize - 1) / wbfsSectorSize));
        long finalPhysicalSize = dataStart + (long)isoBlockCount * wbfsSectorSize;

        BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(0, 4), WbfsMagic);
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(4, 4), checked((uint)(finalPhysicalSize / hdSectorSize)));
        header[8] = WbfsHdSectorShift;
        header[9] = WbfsSectorShift;
        header[12] = 1;
        wbfs.Write(header, 0, header.Length);

        iso.Position = 0;
        ReadExact(iso, discInfo.AsSpan(0, (int)WbfsDiscHeaderSize));
        byte[] buffer = new byte[wbfsSectorSize];
        int blockIndex = 0;
        long remaining = iso.Length;
        iso.Position = 0;
        while (remaining > 0)
        {
            int chunk = (int)Math.Min(buffer.Length, remaining);
            Array.Clear(buffer, 0, buffer.Length);
            ReadExact(iso, buffer.AsSpan(0, chunk));
            BinaryPrimitives.WriteUInt16BigEndian(discInfo.AsSpan((int)WbfsDiscHeaderSize + blockIndex * 2, 2), currentWbfsBlock);
            wbfs.Position = (long)currentWbfsBlock * wbfsSectorSize;
            wbfs.Write(buffer, 0, buffer.Length);
            currentWbfsBlock++;
            blockIndex++;
            remaining -= chunk;
        }

        wbfs.Position = hdSectorSize;
        wbfs.Write(discInfo, 0, discInfo.Length);
        wbfs.SetLength((long)currentWbfsBlock * wbfsSectorSize);
    }

    private static byte[] BuildFst(List<FstEntryData> entries, List<byte> stringTable, int addressShift)
    {
        int totalSize = checked(entries.Count * 12 + stringTable.Count);
        byte[] fst = new byte[totalSize];
        for (int i = 0; i < entries.Count; i++)
        {
            FstEntryData entry = entries[i];
            uint nameField = (entry.IsDirectory ? 0x0100_0000u : 0u) | (uint)entry.NameOffset;
            WriteU32BE(fst, i * 12, nameField);
            WriteU32BE(fst, i * 12 + 4, entry.IsDirectory ? (uint)entry.ParentIndex : (uint)(entry.FileOffset >> addressShift));
            WriteU32BE(fst, i * 12 + 8, entry.IsDirectory ? (uint)entry.NextIndex : (uint)entry.FileSize);
        }

        Buffer.BlockCopy(stringTable.ToArray(), 0, fst, entries.Count * 12, stringTable.Count);
        return fst;
    }

    private static void AssignFileOffsets(List<FstEntryData> entries, uint startOffset, uint alignment)
    {
        uint currentOffset = startOffset;
        foreach (FstEntryData entry in entries.Where(e => !e.IsDirectory))
        {
            currentOffset = Align(currentOffset, alignment);
            entry.FileOffset = currentOffset;
            currentOffset += (uint)entry.FileSize;
        }
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

    private static void WriteBlock(FileStream stream, long offset, byte[] data)
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

    private static void ReadExact(FileStream stream, Span<byte> buffer)
    {
        int readTotal = 0;
        while (readTotal < buffer.Length)
        {
            int read = stream.Read(buffer[readTotal..]);
            if (read == 0)
                throw new EndOfStreamException("Unexpected end of file.");
            readTotal += read;
        }
    }

    private static uint Align(uint value, uint alignment)
    {
        if (alignment == 0)
            return value;
        uint mask = alignment - 1;
        return (value + mask) & ~mask;
    }

    private static int Align(int value, int alignment)
    {
        if (alignment == 0)
            return value;
        int mask = alignment - 1;
        return (value + mask) & ~mask;
    }

    private static long AlignLong(long value, long alignment)
    {
        if (alignment == 0)
            return value;
        long mask = alignment - 1;
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
            throw new InvalidOperationException("Missing required input path.");
        return args[index];
    }

    private static string DefaultOutputPath(string inputFolder, string extension)
    {
        string trimmed = inputFolder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return Path.Combine(Path.GetDirectoryName(trimmed) ?? ".", Path.GetFileName(trimmed) + extension);
    }

    private static void PrintUsage()
    {
        Console.WriteLine("DiscRebuild");
        Console.WriteLine("  GameCube / Wii disc rebuilder for Hocotate Toolkit");
        Console.WriteLine();
        Console.WriteLine("Credits");
        Console.WriteLine("  This tool is part of Hocotate Toolkit by C2H4.");
        Console.WriteLine("  Implementation referenced Dolphin / DolphinTool by jordan-woyak.");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  DiscRebuild.exe <folder> [output.iso|output.wbfs]");
        Console.WriteLine("  DiscRebuild.exe --rebuild <folder> [output.iso|output.wbfs]");
        Console.WriteLine("  DiscRebuild.exe --gcrebuild <folder> [output.iso]");
        Console.WriteLine("  DiscRebuild.exe --wiirebuild <folder> [output.iso|output.wbfs]");
        Console.WriteLine("  DiscRebuild.exe --iso2wbfs <input.iso> [output.wbfs]");
    }

    private enum DiscKind
    {
        Unknown,
        GameCube,
        Wii
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

    private sealed class ContentEntry
    {
        private readonly byte[]? _data;
        private readonly string? _sourcePath;

        public ContentEntry(long offset, byte[] data)
        {
            Offset = offset;
            Size = data.Length;
            _data = data;
        }

        public ContentEntry(long offset, long size, string sourcePath, string path)
        {
            Offset = offset;
            Size = size;
            _sourcePath = sourcePath;
            DisplayPath = path;
        }

        public long Offset { get; }
        public long Size { get; }
        public string DisplayPath { get; } = string.Empty;

        public void CopyTo(long sourceOffset, Span<byte> destination)
        {
            if (_data is not null)
            {
                _data.AsSpan((int)sourceOffset, destination.Length).CopyTo(destination);
                return;
            }

            using FileStream stream = new(_sourcePath!, FileMode.Open, FileAccess.Read, FileShare.Read);
            stream.Position = sourceOffset;
            ReadExact(stream, destination);
        }
    }
}
