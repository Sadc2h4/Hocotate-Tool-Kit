using System;
using System.IO;
using System.Text;

namespace RARCToolkit.IO
{
    /// <summary>
    /// ビッグエンディアンで書き込む BinaryWriter。
    /// GameFormatReader の EndianBinaryWriter と同じインターフェイスを提供します。
    /// </summary>
    public sealed class EndianBinaryWriter : IDisposable
    {
        public Stream BaseStream { get; }

        public EndianBinaryWriter(Stream stream)
        {
            BaseStream = stream;
        }

        public void Write(int value)
        {
            byte[] b = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian) Array.Reverse(b);
            BaseStream.Write(b, 0, 4);
        }

        public void Write(short value)
        {
            byte[] b = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian) Array.Reverse(b);
            BaseStream.Write(b, 0, 2);
        }

        public void Write(byte value)
        {
            BaseStream.WriteByte(value);
        }

        public void Write(byte[] bytes)
        {
            BaseStream.Write(bytes, 0, bytes.Length);
        }

        public void WriteFloat(float value)
        {
            byte[] b = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian) Array.Reverse(b);
            BaseStream.Write(b, 0, 4);
        }

        /// <summary>
        /// 固定長で文字列を書き込む。長さが足りない場合は 0 で埋める。
        /// </summary>
        public void WriteFixedString(string s, int length)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(s);
            int written = Math.Min(bytes.Length, length);
            BaseStream.Write(bytes, 0, written);
            for (int i = written; i < length; i++) BaseStream.WriteByte(0);
        }

        public void Dispose() => BaseStream?.Dispose();
    }
}
