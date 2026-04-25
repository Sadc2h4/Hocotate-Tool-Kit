using System;
using System.Collections.Generic;
using System.IO;
using RARCToolkit.IO;

namespace RARCToolkit.RARC
{
    /// <summary>
    /// フォルダを Nintendo RARC アーカイブにパックする。
    /// RarcPack-master の RARCPacker.cs を GameFormatReader 依存なしに移植。
    /// </summary>
    public class RARCPacker
    {
        private RARCHeader Header = null!;
        private List<RARCNode> Nodes = null!;
        private List<RARCFileEntry> Entries = null!;
        private List<char> StringTable = null!;
        private List<byte> Data = null!;
        private int EntryCount;

        public void Pack(VirtualFolder root, EndianBinaryWriter writer)
        {
            Header = new RARCHeader { Magic = "RARC", Unknown1 = 0x20, Unknown6 = 0x20 };
            Nodes = new List<RARCNode>();
            Entries = new List<RARCFileEntry>();
            StringTable = new List<char>();
            Data = new List<byte>();

            // 文字列テーブルの先頭 "." と ".." を予約
            StringTable.Add('.');  StringTable.Add('\0');
            StringTable.Add('.'); StringTable.Add('.'); StringTable.Add('\0');

            // ルートノードを作成
            RARCNode rootNode = new RARCNode
            {
                Type = "ROOT",
                NameOffset = 5,
                NameHash = HashName(root.Name),
                FileEntryCount = (short)(root.Subdirs.Count + root.Files.Count + 2),
                FirstFileEntryIndex = 0,
            };
            foreach (char c in root.Name) StringTable.Add(c);
            StringTable.Add('\0');

            Nodes.Add(rootNode);
            EntryCount = rootNode.FileEntryCount;

            // サブディレクトリのノードを再帰的に追加
            foreach (VirtualFolder folder in root.Subdirs) RecursiveDir(folder, rootNode);

            // ルートのファイルエントリーを追加
            foreach (FileData file in root.Files) Entries.Add(AddFileEntry(file));

            // ルートの "." ".." エントリー
            Entries.Add(MakeSinglePeriod(0));
            Entries.Add(MakeDoublePeriod(-1));

            // サブディレクトリのファイルエントリーを再帰的に追加
            foreach (VirtualFolder folder in root.Subdirs) RecursiveFile(folder);

            // ヘッダーのオフセット計算
            int headerLength   = 64;
            int alignedNodes   = Align32(Nodes.Count * 16);
            int alignedEntries = Align32(Entries.Count * 20);
            int alignedTable   = alignedNodes + alignedEntries + headerLength;
            int alignedStrSize = Align32(StringTable.Count);
            int alignedData    = Align32(alignedTable + alignedStrSize);

            Header.NodeCount          = Nodes.Count;
            Header.Unknown2           = Data.Count;
            Header.Unknown3           = Data.Count;
            Header.Unknown7           = Entries.Count;
            Header.FileEntryCount     = (short)Entries.Count;
            Header.UnknownBool1       = 1;
            Header.Unknown8           = alignedStrSize;
            Header.FileEntriesOffset  = alignedNodes + 64 - 0x20;
            Header.StringTableOffset  = alignedTable - 0x20;
            Header.DataOffset         = alignedData - 0x20;
            Header.FileSize           = Align32(alignedData + Data.Count);

            Write(writer);
        }

        // ─── プライベートメソッド ────────────────────────────────────

        static int Align32(int value) => (value + 0x1F) & ~0x1F;

        static short HashName(string name)
        {
            short hash = 0;
            short multiplier = name.Length >= 3 ? (short)3 : name.Length == 2 ? (short)2 : (short)1;
            foreach (char c in name) { hash = (short)(hash * multiplier); hash += (short)c; }
            return hash;
        }

        RARCFileEntry AddFileEntry(FileData file)
        {
            var entry = new RARCFileEntry
            {
                FileId     = (short)Entries.Count,
                NameHash   = HashName(file.Name),
                Type       = 0x11,
                NameOffset = (short)StringTable.Count,
                DataOffset = Data.Count,
                DataSize   = file.Data.Length,
            };
            foreach (char c in file.Name) StringTable.Add(c);
            StringTable.Add('\0');
            Data.AddRange(file.Data);
            return entry;
        }

        static RARCFileEntry MakeSinglePeriod(int dataOffset) => new RARCFileEntry
        {
            FileId = -1, NameHash = 0x2E, Type = 2, NameOffset = 0,
            DataOffset = dataOffset, DataSize = 0x10,
        };

        static RARCFileEntry MakeDoublePeriod(int dataOffset) => new RARCFileEntry
        {
            FileId = -1, NameHash = 0xB8, Type = 2, NameOffset = 2,
            DataOffset = dataOffset, DataSize = 0x10,
        };

        void RecursiveDir(VirtualFolder folder, RARCNode parentNode)
        {
            var node = new RARCNode
            {
                Type               = folder.NodeName,
                NameOffset         = StringTable.Count,
                NameHash           = HashName(folder.Name),
                FirstFileEntryIndex = EntryCount,
                FileEntryCount     = (short)(folder.Subdirs.Count + folder.Files.Count + 2),
            };
            foreach (char c in folder.Name) StringTable.Add(c);
            StringTable.Add('\0');

            EntryCount += node.FileEntryCount;
            Nodes.Add(node);

            // 親のエントリーとして subdirEntry を追加
            Entries.Add(new RARCFileEntry
            {
                FileId     = -1,
                NameHash   = HashName(folder.Name),
                Type       = 2,
                NameOffset = (short)node.NameOffset,
                DataOffset = Nodes.IndexOf(node),
                DataSize   = 0x10,
            });

            foreach (VirtualFolder sub in folder.Subdirs) RecursiveDir(sub, node);
        }

        void RecursiveFile(VirtualFolder folder)
        {
            foreach (FileData file in folder.Files) Entries.Add(AddFileEntry(file));
            Entries.Add(MakeSinglePeriod(0));
            Entries.Add(MakeDoublePeriod(0));
            foreach (VirtualFolder sub in folder.Subdirs) RecursiveFile(sub);
        }

        void Write(EndianBinaryWriter w)
        {
            Header.Write(w);
            Pad32(w);
            foreach (var node in Nodes) node.Write(w);
            Pad32(w);
            foreach (var entry in Entries) entry.Write(w);
            Pad32(w);
            foreach (char c in StringTable) w.Write((byte)c);
            Pad32(w);
            w.Write(Data.ToArray());
        }

        static void Pad32(EndianBinaryWriter w)
        {
            long next = (w.BaseStream.Length + 0x1F) & ~0x1F;
            long delta = next - w.BaseStream.Length;
            w.BaseStream.Position = w.BaseStream.Length;
            w.Write(new byte[delta]);
        }
    }
}
