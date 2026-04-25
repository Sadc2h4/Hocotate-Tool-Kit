using System;
using System.Text;

namespace RARCToolkit.Compression
{
    /// <summary>
    /// Yaz0 圧縮形式の展開（Nintendo GameCube/Wii SZS ファイル対応）。
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
    }
}
