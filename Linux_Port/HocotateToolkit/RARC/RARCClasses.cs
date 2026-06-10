using RARCToolkit.IO;

namespace RARCToolkit.RARC
{
    /// <summary>
    /// RARC ヘッダー（64 バイト）。
    /// RarcPack-master の RARCClasses.cs を GameFormatReader 依存なしに移植。
    /// </summary>
    public class RARCHeader
    {
        public string Magic = "RARC";
        public int FileSize;
        public int Unknown1;    // 常に 0x20
        public int DataOffset;

        public int Unknown2;
        public int Unknown3;
        public int Unknown4;
        public int Unknown5;

        public int NodeCount;

        public int Unknown6;    // 常に 0x20
        public int Unknown7;

        public int FileEntriesOffset;
        public int Unknown8;
        public int StringTableOffset;

        public short FileEntryCount;
        public byte UnknownBool1;
        public byte Padding;
        public int Unknown10;

        public void Write(EndianBinaryWriter w)
        {
            w.WriteFixedString(Magic, 4);
            w.Write(FileSize);
            w.Write(Unknown1);
            w.Write(DataOffset);
            w.Write(Unknown2);
            w.Write(Unknown3);
            w.Write(Unknown4);
            w.Write(Unknown5);
            w.Write(NodeCount);
            w.Write(Unknown6);
            w.Write(Unknown7);
            w.Write(FileEntriesOffset);
            w.Write(Unknown8);
            w.Write(StringTableOffset);
            w.Write(FileEntryCount);
            w.Write(UnknownBool1);
            w.Write(Padding);
            w.Write(Unknown10);
        }
    }

    /// <summary>RARC ノード（ディレクトリ情報）16 バイト。</summary>
    public class RARCNode
    {
        public string Type = string.Empty;
        public int NameOffset;
        public short NameHash;
        public short FileEntryCount;
        public int FirstFileEntryIndex;

        public void Write(EndianBinaryWriter w)
        {
            w.WriteFixedString(Type, 4);
            w.Write(NameOffset);
            w.Write(NameHash);
            w.Write(FileEntryCount);
            w.Write(FirstFileEntryIndex);
        }
    }

    /// <summary>RARC ファイルエントリー 20 バイト。</summary>
    public class RARCFileEntry
    {
        public short FileId;
        public short NameHash;
        public byte Type;
        public byte Padding;
        public short NameOffset;
        public int DataOffset;
        public int DataSize;
        public int Zero;

        public void Write(EndianBinaryWriter w)
        {
            w.Write(FileId);
            w.Write(NameHash);
            w.Write(Type);
            w.Write(Padding);
            w.Write(NameOffset);
            w.Write(DataOffset);
            w.Write(DataSize);
            w.Write(Zero);
        }
    }
}
