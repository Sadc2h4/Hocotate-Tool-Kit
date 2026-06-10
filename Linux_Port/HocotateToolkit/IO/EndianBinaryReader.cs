using System;
using System.IO;
using System.Text;

namespace RARCToolkit.IO
{
    /// <summary>
    /// ビッグエンディアンで読み取る BinaryReader。
    /// RARC/SZS ファイルの解析に使用します。
    /// </summary>
    public sealed class EndianBinaryReader : IDisposable
    {
        private readonly Stream _stream;

        public long Position
        {
            get => _stream.Position;
            set => _stream.Position = value;
        }

        public EndianBinaryReader(Stream stream)
        {
            _stream = stream;
        }

        public int ReadInt32()
        {
            byte[] b = ReadBytes(4);
            if (BitConverter.IsLittleEndian) Array.Reverse(b);
            return BitConverter.ToInt32(b, 0);
        }

        public uint ReadUInt32()
        {
            byte[] b = ReadBytes(4);
            if (BitConverter.IsLittleEndian) Array.Reverse(b);
            return BitConverter.ToUInt32(b, 0);
        }

        public short ReadInt16()
        {
            byte[] b = ReadBytes(2);
            if (BitConverter.IsLittleEndian) Array.Reverse(b);
            return BitConverter.ToInt16(b, 0);
        }

        public ushort ReadUInt16()
        {
            byte[] b = ReadBytes(2);
            if (BitConverter.IsLittleEndian) Array.Reverse(b);
            return BitConverter.ToUInt16(b, 0);
        }

        public byte ReadByte() => (byte)_stream.ReadByte();

        public byte[] ReadBytes(int count)
        {
            byte[] buf = new byte[count];
            int read = 0;
            while (read < count)
            {
                int r = _stream.Read(buf, read, count - read);
                if (r == 0) break;
                read += r;
            }
            return buf;
        }

        /// <summary>
        /// 固定長文字列を読み取り、末尾の null を除去して返す。
        /// </summary>
        public string ReadFixedString(int length)
        {
            return Encoding.ASCII.GetString(ReadBytes(length)).TrimEnd('\0');
        }

        public void Dispose() => _stream?.Dispose();
    }
}
