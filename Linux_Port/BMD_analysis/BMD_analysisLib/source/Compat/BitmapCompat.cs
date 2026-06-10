using System;
using System.IO;
using System.Runtime.InteropServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace SuperBMDLib.Compat
{
    // ===============================================================================
    // System.Drawing 互換シム（Linux対応版）
    // System.Drawing.Common は .NET 8 で Windows 専用になったため、
    // SuperBMD が使用する最小限の Bitmap API を SixLabors.ImageSharp で再実装する。
    // ピクセル形式は Format32bppArgb（メモリ上は BGRA 順）のみサポートし、
    // ImageSharp の Bgra32 と同一レイアウトのため変換コストなしでコピーできる。
    // ===============================================================================

    public enum PixelFormat
    {
        Format32bppArgb,
    }

    public enum ImageLockMode
    {
        ReadOnly,
        WriteOnly,
        ReadWrite,
    }

    public enum RotateFlipType
    {
        RotateNoneFlipX,
        RotateNoneFlipY,
        RotateNoneFlipXY,
    }

    /// <summary>
    /// System.Drawing.Imaging.ImageFormat 互換（PNG出力のみ使用）。
    /// </summary>
    public sealed class ImageFormat
    {
        public static ImageFormat Png { get; } = new ImageFormat();
        private ImageFormat() { }
    }

    /// <summary>
    /// System.Drawing.Rectangle 互換の最小実装。
    /// </summary>
    public struct Rectangle
    {
        public int X;
        public int Y;
        public int Width;
        public int Height;

        public Rectangle(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
    }

    /// <summary>
    /// System.Drawing.Imaging.BitmapData 互換。
    /// LockBits 中だけピクセルバッファをピン留めして先頭ポインタを公開する。
    /// </summary>
    public sealed class BitmapData
    {
        public IntPtr Scan0 { get; internal set; }
        public int Stride { get; internal set; }
        internal GCHandle Handle;
    }

    /// <summary>
    /// System.Drawing.Bitmap 互換の最小実装（ImageSharpベース）。
    /// 内部では常に BGRA 32bit のバイト配列としてピクセルを保持する。
    /// </summary>
    public sealed class Bitmap : IDisposable
    {
        private byte[] m_pixels; // BGRA順（Format32bppArgbと同一レイアウト）

        public int Width { get; private set; }
        public int Height { get; private set; }

        //-------------------------------------------------------------------------------
        // 画像ファイルを読み込む処理（PNG/BMP/JPG/GIF/TGAなどImageSharp対応形式）
        // 読込不能な形式は ArgumentException を投げる
        // （呼び出し元が ArgumentException を捕捉して独自TGAローダーへ
        //   フォールバックする既存ロジックを維持するため）
        //-------------------------------------------------------------------------------
        public Bitmap(string filePath)
        {
            try
            {
                using (Image<Bgra32> image = SixLabors.ImageSharp.Image.Load<Bgra32>(filePath))
                {
                    Width = image.Width;
                    Height = image.Height;
                    m_pixels = new byte[Width * Height * 4];
                    image.CopyPixelDataTo(m_pixels);
                }
            }
            catch (UnknownImageFormatException ex)
            {
                throw new ArgumentException($"Unsupported image format: {filePath}", ex);
            }
            catch (InvalidImageContentException ex)
            {
                throw new ArgumentException($"Invalid image content: {filePath}", ex);
            }
        }

        //-------------------------------------------------------------------------------
        // 空のビットマップを作成する処理
        //-------------------------------------------------------------------------------
        public Bitmap(int width, int height)
        {
            Width = width;
            Height = height;
            m_pixels = new byte[width * height * 4];
        }

        public Bitmap(int width, int height, PixelFormat format)
            : this(width, height)
        {
        }

        //-------------------------------------------------------------------------------
        // ピクセルバッファをピン留めして直接アクセス用ポインタを返す処理
        // （System.Drawing の LockBits 互換。Format32bppArgb 固定）
        //-------------------------------------------------------------------------------
        public BitmapData LockBits(Rectangle rect, ImageLockMode mode, PixelFormat format)
        {
            var data = new BitmapData
            {
                Handle = GCHandle.Alloc(m_pixels, GCHandleType.Pinned),
                Stride = Width * 4,
            };
            data.Scan0 = data.Handle.AddrOfPinnedObject();
            return data;
        }

        //-------------------------------------------------------------------------------
        // ピン留めを解除する処理（バッファは共有のため書き戻しは不要）
        //-------------------------------------------------------------------------------
        public void UnlockBits(BitmapData data)
        {
            if (data.Handle.IsAllocated)
                data.Handle.Free();
            data.Scan0 = IntPtr.Zero;
        }

        //-------------------------------------------------------------------------------
        // PNGとして保存する処理
        //-------------------------------------------------------------------------------
        public void Save(string filePath, ImageFormat format)
        {
            using (Image<Bgra32> image = SixLabors.ImageSharp.Image.LoadPixelData<Bgra32>(m_pixels, Width, Height))
            {
                image.SaveAsPng(filePath);
            }
        }

        //-------------------------------------------------------------------------------
        // 上下左右の反転を行う処理（TGAの向きフラグ対応用）
        //-------------------------------------------------------------------------------
        public void RotateFlip(RotateFlipType type)
        {
            if (type == RotateFlipType.RotateNoneFlipX || type == RotateFlipType.RotateNoneFlipXY)
                FlipX();
            if (type == RotateFlipType.RotateNoneFlipY || type == RotateFlipType.RotateNoneFlipXY)
                FlipY();
        }

        //-------------------------------------------------------------------------------
        // 左右反転（各行内のピクセル順を逆にする）処理
        //-------------------------------------------------------------------------------
        private void FlipX()
        {
            var tmp = new byte[4];
            for (int y = 0; y < Height; y++)
            {
                int rowStart = y * Width * 4;
                for (int x = 0; x < Width / 2; x++)
                {
                    int left = rowStart + x * 4;
                    int right = rowStart + (Width - 1 - x) * 4;
                    Array.Copy(m_pixels, left, tmp, 0, 4);
                    Array.Copy(m_pixels, right, m_pixels, left, 4);
                    Array.Copy(tmp, 0, m_pixels, right, 4);
                }
            }
        }

        //-------------------------------------------------------------------------------
        // 上下反転（行の並びを逆にする）処理
        //-------------------------------------------------------------------------------
        private void FlipY()
        {
            int stride = Width * 4;
            var tmpRow = new byte[stride];
            for (int y = 0; y < Height / 2; y++)
            {
                int top = y * stride;
                int bottom = (Height - 1 - y) * stride;
                Array.Copy(m_pixels, top, tmpRow, 0, stride);
                Array.Copy(m_pixels, bottom, m_pixels, top, stride);
                Array.Copy(tmpRow, 0, m_pixels, bottom, stride);
            }
        }

        public void Dispose()
        {
            // マネージド配列のみ保持しているため解放処理は不要
        }
    }
}
