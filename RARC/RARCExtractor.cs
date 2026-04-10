using System;
using System.IO;
using System.Text;
using RARCToolkit.Compression;
using RARCToolkit.IO;

namespace RARCToolkit.RARC
{
    /// <summary>
    /// Nintendo RARC / SZS（Yaz0 圧縮 RARC）アーカイブを展開する。
    /// </summary>
    public class RARCExtractor
    {
        public void Extract(string inputPath, string outputDir)
        {
            byte[] raw = File.ReadAllBytes(inputPath);

            // Yaz0 圧縮されている場合は展開
            if (Yaz0.IsYaz0(raw))
            {
                Console.WriteLine("  Yaz0 圧縮を検出 — 展開中...");
                raw = Yaz0.Decompress(raw);
            }

            // RARC マジック確認
            string magic = Encoding.ASCII.GetString(raw, 0, 4);
            if (magic != "RARC")
                throw new InvalidDataException($"RARC ファイルではありません (magic: {magic})");

            using var ms = new MemoryStream(raw);
            using var r  = new EndianBinaryReader(ms);

            // ── ヘッダー読み取り (64 バイト) ──────────────────────────
            r.ReadFixedString(4);           // Magic "RARC"
            r.ReadInt32();                  // FileSize
            r.ReadInt32();                  // Unknown1 (0x20)
            int dataOffsetRel      = r.ReadInt32();
            r.ReadInt32(); r.ReadInt32(); r.ReadInt32(); r.ReadInt32(); // Unknown2-5
            int nodeCount          = r.ReadInt32();
            r.ReadInt32();                  // Unknown6 (0x20)
            r.ReadInt32();                  // Unknown7
            int entryOffsetRel     = r.ReadInt32();
            int stringTableSize    = r.ReadInt32();
            int stringTableOffRel  = r.ReadInt32();
            int fileEntryCount     = r.ReadInt16();
            r.ReadByte(); r.ReadByte(); r.ReadInt32(); // UnknownBool1, Padding, Unknown10

            // オフセットは 0x20 からの相対値
            long nodesAbsOffset       = 64;                       // ヘッダーの直後
            long entriesAbsOffset     = 0x20 + entryOffsetRel;
            long stringTableAbsOffset = 0x20 + stringTableOffRel;
            long dataAbsOffset        = 0x20 + dataOffsetRel;

            // ── ノード読み取り ──────────────────────────────────────────
            r.Position = nodesAbsOffset;
            var nodes = new NodeInfo[nodeCount];
            for (int i = 0; i < nodeCount; i++)
            {
                nodes[i] = new NodeInfo
                {
                    Type                = r.ReadFixedString(4),
                    NameOffset          = r.ReadInt32(),
                    NameHash            = r.ReadInt16(),
                    FileEntryCount      = r.ReadInt16(),
                    FirstFileEntryIndex = r.ReadInt32(),
                };
            }

            // ── ファイルエントリー読み取り ───────────────────────────────
            r.Position = entriesAbsOffset;
            var entries = new EntryInfo[fileEntryCount];
            for (int i = 0; i < fileEntryCount; i++)
            {
                entries[i] = new EntryInfo
                {
                    FileId     = r.ReadInt16(),
                    NameHash   = r.ReadInt16(),
                    Type       = r.ReadByte(),
                    Padding    = r.ReadByte(),
                    NameOffset = r.ReadInt16(),
                    DataOffset = r.ReadInt32(),
                    DataSize   = r.ReadInt32(),
                };
                r.ReadInt32(); // Zero
            }

            // ── 文字列テーブル読み取り ──────────────────────────────────
            r.Position = stringTableAbsOffset;
            byte[] strTable = r.ReadBytes(stringTableSize);

            string GetStr(int offset)
            {
                int end = offset;
                while (end < strTable.Length && strTable[end] != 0) end++;
                return Encoding.ASCII.GetString(strTable, offset, end - offset);
            }

            // ── ルートノードから再帰展開 ────────────────────────────────
            Directory.CreateDirectory(outputDir);
            ExtractNode(nodes, entries, raw, (int)dataAbsOffset, GetStr, 0, outputDir);

            Console.WriteLine($"  展開完了: {outputDir}");
        }

        // ─── プライベートメソッド ────────────────────────────────────

        private void ExtractNode(
            NodeInfo[] nodes, EntryInfo[] entries,
            byte[] raw, int dataAbsOffset,
            Func<int, string> getString,
            int nodeIndex, string currentDir)
        {
            if (nodeIndex < 0 || nodeIndex >= nodes.Length) return;

            NodeInfo node = nodes[nodeIndex];
            int start = node.FirstFileEntryIndex;
            int end   = start + node.FileEntryCount;

            for (int i = start; i < end && i < entries.Length; i++)
            {
                EntryInfo entry = entries[i];
                string name = getString(entry.NameOffset);

                if (name == "." || name == "..") continue;

                if ((entry.Type & 0x02) != 0)
                {
                    // ディレクトリ
                    string subDir = Path.Combine(currentDir, name);
                    Directory.CreateDirectory(subDir);
                    ExtractNode(nodes, entries, raw, dataAbsOffset, getString, entry.DataOffset, subDir);
                }
                else
                {
                    // ファイル
                    string filePath  = Path.Combine(currentDir, name);
                    int    fileStart = dataAbsOffset + entry.DataOffset;
                    int    fileEnd   = fileStart + entry.DataSize;

                    if (fileEnd > raw.Length)
                        throw new InvalidDataException($"ファイルデータが範囲外: {name}");

                    File.WriteAllBytes(filePath, raw[fileStart..fileEnd]);
                    Console.WriteLine($"  展開: {filePath}");
                }
            }
        }

        // ─── 内部データ構造 ──────────────────────────────────────────

        private class NodeInfo
        {
            public string Type                = string.Empty;
            public int    NameOffset;
            public short  NameHash;
            public short  FileEntryCount;
            public int    FirstFileEntryIndex;
        }

        private class EntryInfo
        {
            public short FileId;
            public short NameHash;
            public byte  Type;
            public byte  Padding;
            public short NameOffset;
            public int   DataOffset;
            public int   DataSize;
        }
    }
}
