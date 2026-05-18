using System;

using fin.image.formats;
using fin.math;

using SixLabors.ImageSharp.PixelFormats;

namespace fin.image.io.pixel;

/// <summary>
///   Helper class for reading 4-bit intensity pixels.
/// </summary>
public sealed class I4PixelReader : IPixelReader<La16> {
  public IImage<La16> CreateImage(int width, int height)
    => new I8Image(PixelFormat.I4, width, height);

  public void Decode(ReadOnlySpan<byte> data, Span<La16> scan0, int offset) {
    var value = data[0];

    var upper = BitLogic.Expand4To8((value >> 4) & 0xF);
    var lower = BitLogic.Expand4To8(value & 0xF);

    scan0[offset + 0] = new La16(upper, upper);
    scan0[offset + 1] = new La16(lower, lower);
  }

  public int PixelsPerRead => 2;
  public int BitsPerPixel => 4;
}