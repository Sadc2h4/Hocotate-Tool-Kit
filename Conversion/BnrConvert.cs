using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace RARCToolkit.Conversion
{
    /// <summary>
    /// Nintendo GameCube BNR banner image and PNG image conversion.
    /// </summary>
    public static class BnrConvert
    {
        private const int Width = 96;
        private const int Height = 32;
        private const int HeaderSize = 0x20;
        private const int ImageSize = Width * Height * 2;
        private const int InfoBlockSize = 0x140;
        private static readonly byte[] PngSignature = { 137, 80, 78, 71, 13, 10, 26, 10 };

        //-------------------------------------------------------------------------------
        // BNRファイルからPNG画像を展開する処理
        //-------------------------------------------------------------------------------
        public static void ExtractImage(string inputBnr, string outputPng)
        {
            byte[] data = File.ReadAllBytes(inputBnr);
            if (data.Length < HeaderSize + ImageSize)
                throw new InvalidDataException("BNR file is too small.");

            string magic = Encoding.ASCII.GetString(data, 0, 4);
            if (magic != "BNR1" && magic != "BNR2")
                throw new InvalidDataException($"Input file is not a BNR file: {magic}");

            Rgba32[] pixels = DecodeRgb5A3Image(data.AsSpan(HeaderSize, ImageSize));
            PngImage.Write(outputPng, Width, Height, pixels);
        }

        //-------------------------------------------------------------------------------
        // PNG画像からBNR1ファイルを作成する処理
        //-------------------------------------------------------------------------------
        public static void PackImage(string inputPng, string outputBnr)
        {
            PngImage image = PngImage.Read(inputPng);
            if (image.Width != Width || image.Height != Height)
                throw new InvalidDataException($"BNR banner PNG must be exactly {Width}x{Height} pixels.");

            using var output = new MemoryStream();
            output.Write(Encoding.ASCII.GetBytes("BNR1"));
            output.Write(new byte[HeaderSize - 4]);
            EncodeRgb5A3Image(image.Pixels, output);
            output.Write(new byte[InfoBlockSize]);
            File.WriteAllBytes(outputBnr, output.ToArray());
        }

        //-------------------------------------------------------------------------------
        // GX RGB5A3タイル画像をRGBA画素へ変換する処理
        //-------------------------------------------------------------------------------
        private static Rgba32[] DecodeRgb5A3Image(ReadOnlySpan<byte> imageData)
        {
            var pixels = new Rgba32[Width * Height];
            int source = 0;

            for (int blockY = 0; blockY < Height; blockY += 4)
            {
                for (int blockX = 0; blockX < Width; blockX += 4)
                {
                    for (int y = 0; y < 4; y++)
                    {
                        for (int x = 0; x < 4; x++)
                        {
                            ushort value = (ushort)((imageData[source] << 8) | imageData[source + 1]);
                            source += 2;
                            pixels[(blockY + y) * Width + blockX + x] = DecodeRgb5A3Pixel(value);
                        }
                    }
                }
            }

            return pixels;
        }

        //-------------------------------------------------------------------------------
        // RGBA画素をGX RGB5A3タイル画像として書き込む処理
        //-------------------------------------------------------------------------------
        private static void EncodeRgb5A3Image(Rgba32[] pixels, Stream output)
        {
            for (int blockY = 0; blockY < Height; blockY += 4)
            {
                for (int blockX = 0; blockX < Width; blockX += 4)
                {
                    for (int y = 0; y < 4; y++)
                    {
                        for (int x = 0; x < 4; x++)
                        {
                            ushort value = EncodeRgb5A3Pixel(pixels[(blockY + y) * Width + blockX + x]);
                            output.WriteByte((byte)(value >> 8));
                            output.WriteByte((byte)value);
                        }
                    }
                }
            }
        }

        //-------------------------------------------------------------------------------
        // RGB5A3の1画素をRGBAへ変換する処理
        //-------------------------------------------------------------------------------
        private static Rgba32 DecodeRgb5A3Pixel(ushort value)
        {
            if ((value & 0x8000) != 0)
            {
                byte r = Expand5((value >> 10) & 0x1F);
                byte g = Expand5((value >> 5) & 0x1F);
                byte b = Expand5(value & 0x1F);
                return new Rgba32(r, g, b, 255);
            }
            else
            {
                byte a = Expand3((value >> 12) & 0x07);
                byte r = Expand4((value >> 8) & 0x0F);
                byte g = Expand4((value >> 4) & 0x0F);
                byte b = Expand4(value & 0x0F);
                return new Rgba32(r, g, b, a);
            }
        }

        //-------------------------------------------------------------------------------
        // RGBAの1画素をRGB5A3へ変換する処理
        //-------------------------------------------------------------------------------
        private static ushort EncodeRgb5A3Pixel(Rgba32 pixel)
        {
            if (pixel.A < 255)
            {
                return (ushort)(((pixel.A >> 5) << 12) |
                                ((pixel.R >> 4) << 8) |
                                ((pixel.G >> 4) << 4) |
                                (pixel.B >> 4));
            }

            return (ushort)(0x8000 |
                            ((pixel.R >> 3) << 10) |
                            ((pixel.G >> 3) << 5) |
                            (pixel.B >> 3));
        }

        //-------------------------------------------------------------------------------
        // 3bit値を8bit値へ拡張する処理
        //-------------------------------------------------------------------------------
        private static byte Expand3(int value)
            => (byte)((value << 5) | (value << 2) | (value >> 1));

        //-------------------------------------------------------------------------------
        // 4bit値を8bit値へ拡張する処理
        //-------------------------------------------------------------------------------
        private static byte Expand4(int value)
            => (byte)((value << 4) | value);

        //-------------------------------------------------------------------------------
        // 5bit値を8bit値へ拡張する処理
        //-------------------------------------------------------------------------------
        private static byte Expand5(int value)
            => (byte)((value << 3) | (value >> 2));

        private readonly record struct Rgba32(byte R, byte G, byte B, byte A);

        private sealed class PngImage
        {
            public int Width { get; private init; }
            public int Height { get; private init; }
            public Rgba32[] Pixels { get; private init; } = Array.Empty<Rgba32>();

            //-------------------------------------------------------------------------------
            // PNG画像を読み込む処理
            //-------------------------------------------------------------------------------
            public static PngImage Read(string path)
            {
                byte[] data = File.ReadAllBytes(path);
                if (data.Length < PngSignature.Length || !data.AsSpan(0, PngSignature.Length).SequenceEqual(PngSignature))
                    throw new InvalidDataException("Input file is not a PNG file.");

                int width = 0;
                int height = 0;
                byte bitDepth = 0;
                byte colorType = 0;
                byte interlace = 0;
                byte[] palette = Array.Empty<byte>();
                byte[] transparency = Array.Empty<byte>();
                using var idat = new MemoryStream();

                int offset = PngSignature.Length;
                while (offset + 12 <= data.Length)
                {
                    uint length = ReadUInt32(data, offset);
                    string type = Encoding.ASCII.GetString(data, offset + 4, 4);
                    int chunkData = offset + 8;
                    if (chunkData + length + 4 > data.Length)
                        throw new InvalidDataException("PNG chunk exceeds file size.");

                    switch (type)
                    {
                        case "IHDR":
                            width = (int)ReadUInt32(data, chunkData);
                            height = (int)ReadUInt32(data, chunkData + 4);
                            bitDepth = data[chunkData + 8];
                            colorType = data[chunkData + 9];
                            interlace = data[chunkData + 12];
                            break;
                        case "PLTE":
                            palette = data[chunkData..(chunkData + (int)length)];
                            break;
                        case "tRNS":
                            transparency = data[chunkData..(chunkData + (int)length)];
                            break;
                        case "IDAT":
                            idat.Write(data, chunkData, (int)length);
                            break;
                        case "IEND":
                            offset = data.Length;
                            continue;
                    }

                    offset = chunkData + (int)length + 4;
                }

                if (width <= 0 || height <= 0)
                    throw new InvalidDataException("PNG does not contain a valid IHDR chunk.");
                if (bitDepth != 8)
                    throw new InvalidDataException("Only 8-bit PNG files are supported.");
                if (interlace != 0)
                    throw new InvalidDataException("Interlaced PNG files are not supported.");

                byte[] raw = Inflate(idat.ToArray());
                return new PngImage
                {
                    Width = width,
                    Height = height,
                    Pixels = DecodeScanlines(raw, width, height, colorType, palette, transparency),
                };
            }

            //-------------------------------------------------------------------------------
            // RGBA画素をPNG画像として書き込む処理
            //-------------------------------------------------------------------------------
            public static void Write(string path, int width, int height, Rgba32[] pixels)
            {
                if (pixels.Length != width * height)
                    throw new ArgumentException("Pixel count does not match PNG dimensions.");

                using var output = new MemoryStream();
                output.Write(PngSignature);
                WriteChunk(output, "IHDR", BuildIhdr(width, height));

                using var scanlines = new MemoryStream();
                for (int y = 0; y < height; y++)
                {
                    scanlines.WriteByte(0);
                    for (int x = 0; x < width; x++)
                    {
                        Rgba32 pixel = pixels[y * width + x];
                        scanlines.WriteByte(pixel.R);
                        scanlines.WriteByte(pixel.G);
                        scanlines.WriteByte(pixel.B);
                        scanlines.WriteByte(pixel.A);
                    }
                }

                WriteChunk(output, "IDAT", Deflate(scanlines.ToArray()));
                WriteChunk(output, "IEND", Array.Empty<byte>());
                File.WriteAllBytes(path, output.ToArray());
            }

            //-------------------------------------------------------------------------------
            // PNGスキャンラインをRGBA画素へ変換する処理
            //-------------------------------------------------------------------------------
            private static Rgba32[] DecodeScanlines(byte[] raw, int width, int height, byte colorType, byte[] palette, byte[] transparency)
            {
                int bytesPerPixel = colorType switch
                {
                    0 => 1,
                    2 => 3,
                    3 => 1,
                    4 => 2,
                    6 => 4,
                    _ => throw new InvalidDataException($"Unsupported PNG color type: {colorType}"),
                };
                int stride = width * bytesPerPixel;
                int source = 0;
                byte[] previous = new byte[stride];
                byte[] current = new byte[stride];
                var pixels = new Rgba32[width * height];

                for (int y = 0; y < height; y++)
                {
                    if (source >= raw.Length)
                        throw new InvalidDataException("PNG image data ended unexpectedly.");

                    byte filter = raw[source++];
                    if (source + stride > raw.Length)
                        throw new InvalidDataException("PNG scanline exceeds image data size.");

                    Array.Copy(raw, source, current, 0, stride);
                    source += stride;
                    Unfilter(current, previous, bytesPerPixel, filter);
                    CopyPixels(current, pixels, y * width, width, colorType, palette, transparency);
                    (previous, current) = (current, previous);
                    Array.Clear(current, 0, current.Length);
                }

                return pixels;
            }

            //-------------------------------------------------------------------------------
            // PNGフィルタを解除する処理
            //-------------------------------------------------------------------------------
            private static void Unfilter(byte[] current, byte[] previous, int bytesPerPixel, byte filter)
            {
                for (int i = 0; i < current.Length; i++)
                {
                    int left = i >= bytesPerPixel ? current[i - bytesPerPixel] : 0;
                    int up = previous[i];
                    int upLeft = i >= bytesPerPixel ? previous[i - bytesPerPixel] : 0;
                    int add = filter switch
                    {
                        0 => 0,
                        1 => left,
                        2 => up,
                        3 => (left + up) / 2,
                        4 => Paeth(left, up, upLeft),
                        _ => throw new InvalidDataException($"Unsupported PNG filter type: {filter}"),
                    };
                    current[i] = (byte)((current[i] + add) & 0xFF);
                }
            }

            //-------------------------------------------------------------------------------
            // スキャンラインの色値をRGBAへコピーする処理
            //-------------------------------------------------------------------------------
            private static void CopyPixels(byte[] scanline, Rgba32[] pixels, int pixelOffset, int width, byte colorType, byte[] palette, byte[] transparency)
            {
                for (int x = 0; x < width; x++)
                {
                    pixels[pixelOffset + x] = colorType switch
                    {
                        0 => new Rgba32(scanline[x], scanline[x], scanline[x], 255),
                        2 => new Rgba32(scanline[x * 3], scanline[x * 3 + 1], scanline[x * 3 + 2], GetTruecolorAlpha(scanline, x, transparency)),
                        3 => GetPalettePixel(scanline[x], palette, transparency),
                        4 => new Rgba32(scanline[x * 2], scanline[x * 2], scanline[x * 2], scanline[x * 2 + 1]),
                        6 => new Rgba32(scanline[x * 4], scanline[x * 4 + 1], scanline[x * 4 + 2], scanline[x * 4 + 3]),
                        _ => throw new InvalidDataException($"Unsupported PNG color type: {colorType}"),
                    };
                }
            }

            //-------------------------------------------------------------------------------
            // パレットPNGの色値を取得する処理
            //-------------------------------------------------------------------------------
            private static Rgba32 GetPalettePixel(byte index, byte[] palette, byte[] transparency)
            {
                int offset = index * 3;
                if (offset + 2 >= palette.Length)
                    throw new InvalidDataException("PNG palette index is out of range.");

                byte alpha = index < transparency.Length ? transparency[index] : (byte)255;
                return new Rgba32(palette[offset], palette[offset + 1], palette[offset + 2], alpha);
            }

            //-------------------------------------------------------------------------------
            // tRNSチャンクからRGB PNGの透過値を取得する処理
            //-------------------------------------------------------------------------------
            private static byte GetTruecolorAlpha(byte[] scanline, int x, byte[] transparency)
            {
                if (transparency.Length < 6)
                    return 255;

                ushort tr = (ushort)((transparency[0] << 8) | transparency[1]);
                ushort tg = (ushort)((transparency[2] << 8) | transparency[3]);
                ushort tb = (ushort)((transparency[4] << 8) | transparency[5]);
                return scanline[x * 3] == tr && scanline[x * 3 + 1] == tg && scanline[x * 3 + 2] == tb ? (byte)0 : (byte)255;
            }

            //-------------------------------------------------------------------------------
            // PNGのPaeth予測値を計算する処理
            //-------------------------------------------------------------------------------
            private static int Paeth(int left, int up, int upLeft)
            {
                int p = left + up - upLeft;
                int pa = Math.Abs(p - left);
                int pb = Math.Abs(p - up);
                int pc = Math.Abs(p - upLeft);
                if (pa <= pb && pa <= pc)
                    return left;
                return pb <= pc ? up : upLeft;
            }

            //-------------------------------------------------------------------------------
            // PNG IHDRチャンクを作成する処理
            //-------------------------------------------------------------------------------
            private static byte[] BuildIhdr(int width, int height)
            {
                byte[] ihdr = new byte[13];
                WriteUInt32(ihdr, 0, (uint)width);
                WriteUInt32(ihdr, 4, (uint)height);
                ihdr[8] = 8;
                ihdr[9] = 6;
                return ihdr;
            }

            //-------------------------------------------------------------------------------
            // zlibデータを展開する処理
            //-------------------------------------------------------------------------------
            private static byte[] Inflate(byte[] data)
            {
                using var input = new MemoryStream(data);
                using var zlib = new ZLibStream(input, CompressionMode.Decompress);
                using var output = new MemoryStream();
                zlib.CopyTo(output);
                return output.ToArray();
            }

            //-------------------------------------------------------------------------------
            // zlibデータへ圧縮する処理
            //-------------------------------------------------------------------------------
            private static byte[] Deflate(byte[] data)
            {
                using var output = new MemoryStream();
                using (var zlib = new ZLibStream(output, CompressionLevel.Optimal, leaveOpen: true))
                    zlib.Write(data);
                return output.ToArray();
            }

            //-------------------------------------------------------------------------------
            // PNGチャンクを書き込む処理
            //-------------------------------------------------------------------------------
            private static void WriteChunk(Stream output, string type, byte[] data)
            {
                byte[] typeBytes = Encoding.ASCII.GetBytes(type);
                WriteUInt32(output, (uint)data.Length);
                output.Write(typeBytes);
                output.Write(data);

                byte[] crcData = new byte[typeBytes.Length + data.Length];
                typeBytes.CopyTo(crcData, 0);
                data.CopyTo(crcData, typeBytes.Length);
                WriteUInt32(output, Crc32.Compute(crcData));
            }

            //-------------------------------------------------------------------------------
            // UInt32をビッグエンディアンで読み込む処理
            //-------------------------------------------------------------------------------
            private static uint ReadUInt32(byte[] data, int offset)
                => (uint)((data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3]);

            //-------------------------------------------------------------------------------
            // UInt32をビッグエンディアンで配列へ書き込む処理
            //-------------------------------------------------------------------------------
            private static void WriteUInt32(byte[] data, int offset, uint value)
            {
                data[offset] = (byte)(value >> 24);
                data[offset + 1] = (byte)(value >> 16);
                data[offset + 2] = (byte)(value >> 8);
                data[offset + 3] = (byte)value;
            }

            //-------------------------------------------------------------------------------
            // UInt32をビッグエンディアンでストリームへ書き込む処理
            //-------------------------------------------------------------------------------
            private static void WriteUInt32(Stream stream, uint value)
            {
                stream.WriteByte((byte)(value >> 24));
                stream.WriteByte((byte)(value >> 16));
                stream.WriteByte((byte)(value >> 8));
                stream.WriteByte((byte)value);
            }
        }

        private static class Crc32
        {
            private static readonly uint[] Table = BuildTable();

            //-------------------------------------------------------------------------------
            // CRC32値を計算する処理
            //-------------------------------------------------------------------------------
            public static uint Compute(byte[] data)
            {
                uint crc = 0xFFFFFFFF;
                foreach (byte value in data)
                    crc = Table[(crc ^ value) & 0xFF] ^ (crc >> 8);
                return crc ^ 0xFFFFFFFF;
            }

            //-------------------------------------------------------------------------------
            // CRC32テーブルを作成する処理
            //-------------------------------------------------------------------------------
            private static uint[] BuildTable()
            {
                var table = new uint[256];
                for (uint i = 0; i < table.Length; i++)
                {
                    uint crc = i;
                    for (int bit = 0; bit < 8; bit++)
                        crc = (crc & 1) != 0 ? 0xEDB88320 ^ (crc >> 1) : crc >> 1;
                    table[i] = crc;
                }

                return table;
            }
        }
    }
}
