using System.IO;
using System;

namespace fin.util.streams;

public static class StreamExtensions {
  public static void CopyTo(
      this Stream src,
      uint srcOffset,
      int srcLength,
      Stream dst) {
    var bufferSize = (int) Math.Min(src.Length - srcOffset, srcLength);
    Span<byte> buffer = stackalloc byte[bufferSize];

    var remainingLength = srcLength;
    while (remainingLength > 0) {
      var currentLength = Math.Min(remainingLength, bufferSize);
      var currentBuffer = buffer[..currentLength];
      remainingLength -= bufferSize;

      var tmp = src.Position;
      src.Position = srcOffset;
      src.ReadExactly(currentBuffer);
      src.Position = tmp;

      dst.Write(currentBuffer);
      srcOffset += (uint) currentLength;
    }
  }

  public static void CopyFromMiddleToEnd(
      this Stream stream,
      uint srcOffset,
      int srcLength)
    => stream.CopyTo(srcOffset, srcLength, stream);

  public static byte[] ReadAllBytes(this Stream stream) {
    var bytes = new byte[stream.Length];
    stream.ReadExactly(bytes);
    return bytes;
  }
}