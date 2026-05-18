using System;

using fin.image.formats;
using fin.math;

using SixLabors.ImageSharp.PixelFormats;


namespace fin.image.io.pixel;

/// <summary>
///   Helper class for reading 4-bit luminance pixels.
/// </summary>
public sealed class L4PixelReader : IPixelReader<L8> {
  public IImage<L8> CreateImage(int width, int height)
    => new L8Image(PixelFormat.L4, width, height);

  public void Decode(ReadOnlySpan<byte> data, Span<L8> scan0, int offset) {
    var value = data[0];

    var upper = BitLogic.Expand4To8((value >> 4) & 0xF);
    var lower = BitLogic.Expand4To8(value & 0xF);

    scan0[offset + 0] = new L8(upper);
    scan0[offset + 1] = new L8(lower);
  }

  public int PixelsPerRead => 2;
  public int BitsPerPixel => 4;
}