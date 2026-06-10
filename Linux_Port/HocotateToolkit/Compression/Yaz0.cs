using System;
using System.Collections.Generic;
using System.Text;

namespace RARCToolkit.Compression
{
    /// <summary>
    /// Yaz0 圧縮形式の展開と圧縮（Nintendo GameCube/Wii SZS ファイル対応）。
    /// </summary>
    public static class Yaz0
    {
        private static readonly byte[] MagicBytes = Encoding.ASCII.GetBytes("Yaz0");

        public static bool IsYaz0(byte[] data)
        {
            if (data.Length < 16) return false;
            return data[0] == 'Y' && data[1] == 'a' && data[2] == 'z' && data[3] == '0';
        }

        /// <summary>
        /// Yaz0 圧縮データを展開して返す。
        /// Yaz0 でない場合はそのまま返す。
        /// </summary>
        public static byte[] Decompress(byte[] src)
        {
            if (!IsYaz0(src)) return src;

            // ヘッダー: magic(4) + uncompressedSize(4) + reserved(8) = 16 bytes
            int uncompressedSize = (src[4] << 24) | (src[5] << 16) | (src[6] << 8) | src[7];
            byte[] dst = new byte[uncompressedSize];

            int srcPos = 16;
            int dstPos = 0;

            while (dstPos < uncompressedSize && srcPos < src.Length)
            {
                byte codeByte = src[srcPos++];

                for (int bit = 7; bit >= 0; bit--)
                {
                    if (dstPos >= uncompressedSize || srcPos >= src.Length) break;

                    if ((codeByte & (1 << bit)) != 0)
                    {
                        // リテラルコピー
                        dst[dstPos++] = src[srcPos++];
                    }
                    else
                    {
                        // バック参照
                        if (srcPos + 1 >= src.Length) break;
                        byte b1 = src[srcPos++];
                        byte b2 = src[srcPos++];

                        int dist = ((b1 & 0x0F) << 8) | b2;
                        int copyPos = dstPos - dist - 1;

                        int length;
                        int nibble = (b1 >> 4) & 0x0F;
                        if (nibble == 0)
                        {
                            if (srcPos >= src.Length) break;
                            length = src[srcPos++] + 18;
                        }
                        else
                        {
                            length = nibble + 2;
                        }

                        for (int i = 0; i < length && dstPos < uncompressedSize; i++)
                        {
                            dst[dstPos++] = dst[copyPos++];
                        }
                    }
                }
            }

            return dst;
        }

        //-------------------------------------------------------------------------------
        // Yaz0 圧縮データを作成して返す処理
        //-------------------------------------------------------------------------------
        public static byte[] Compress(byte[] src)
        {
            using var output = new System.IO.MemoryStream();
            output.Write(MagicBytes);
            WriteUInt32BE(output, (uint)src.Length);
            output.Write(new byte[8]);

            var positionsByKey = new Dictionary<int, Queue<int>>();
            int srcPos = 0;

            while (srcPos < src.Length)
            {
                long codePos = output.Position;
                output.WriteByte(0);
                byte codeByte = 0;

                for (int bit = 7; bit >= 0 && srcPos < src.Length; bit--)
                {
                    Match match = FindMatch(src, srcPos, positionsByKey);
                    if (match.Length >= 3)
                    {
                        int distance = srcPos - match.Position - 1;
                        if (match.Length >= 18)
                        {
                            output.WriteByte((byte)(distance >> 8));
                            output.WriteByte((byte)distance);
                            output.WriteByte((byte)(match.Length - 18));
                        }
                        else
                        {
                            output.WriteByte((byte)(((match.Length - 2) << 4) | (distance >> 8)));
                            output.WriteByte((byte)distance);
                        }

                        int end = srcPos + match.Length;
                        while (srcPos < end)
                        {
                            AddSearchPosition(src, srcPos, positionsByKey);
                            srcPos++;
                        }
                    }
                    else
                    {
                        codeByte |= (byte)(1 << bit);
                        output.WriteByte(src[srcPos]);
                        AddSearchPosition(src, srcPos, positionsByKey);
                        srcPos++;
                    }
                }

                long endPos = output.Position;
                output.Position = codePos;
                output.WriteByte(codeByte);
                output.Position = endPos;
            }

            return output.ToArray();
        }

        //-------------------------------------------------------------------------------
        // Yaz0 の検索辞書へ現在位置を追加する処理
        //-------------------------------------------------------------------------------
        private static void AddSearchPosition(byte[] src, int pos, Dictionary<int, Queue<int>> positionsByKey)
        {
            if (pos + 2 >= src.Length)
                return;

            int key = (src[pos] << 16) | (src[pos + 1] << 8) | src[pos + 2];
            if (!positionsByKey.TryGetValue(key, out Queue<int>? positions))
            {
                positions = new Queue<int>();
                positionsByKey[key] = positions;
            }

            positions.Enqueue(pos);
            while (positions.Count > 0 && pos - positions.Peek() > 0x1000)
                positions.Dequeue();
        }

        //-------------------------------------------------------------------------------
        // 現在位置から最長一致する過去データを探す処理
        //-------------------------------------------------------------------------------
        private static Match FindMatch(byte[] src, int pos, Dictionary<int, Queue<int>> positionsByKey)
        {
            if (pos + 2 >= src.Length)
                return default;

            int key = (src[pos] << 16) | (src[pos + 1] << 8) | src[pos + 2];
            if (!positionsByKey.TryGetValue(key, out Queue<int>? positions))
                return default;

            while (positions.Count > 0 && pos - positions.Peek() > 0x1000)
                positions.Dequeue();

            if (positions.Count == 0)
                return default;

            int bestPos = 0;
            int bestLength = 0;
            int maxLength = Math.Min(273, src.Length - pos);

            foreach (int candidate in positions)
            {
                int candidateMaxLength = Math.Min(maxLength, pos - candidate);
                int length = 0;
                while (length < candidateMaxLength && src[candidate + length] == src[pos + length])
                    length++;

                if (length > bestLength)
                {
                    bestLength = length;
                    bestPos = candidate;
                    if (bestLength == maxLength)
                        break;
                }
            }

            return new Match(bestPos, bestLength);
        }

        //-------------------------------------------------------------------------------
        // UInt32をビッグエンディアンで書き込む処理
        //-------------------------------------------------------------------------------
        private static void WriteUInt32BE(System.IO.Stream stream, uint value)
        {
            stream.WriteByte((byte)(value >> 24));
            stream.WriteByte((byte)(value >> 16));
            stream.WriteByte((byte)(value >> 8));
            stream.WriteByte((byte)value);
        }

        private readonly record struct Match(int Position, int Length);
    }
}
