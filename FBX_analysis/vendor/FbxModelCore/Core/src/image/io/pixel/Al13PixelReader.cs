using System;
using System.Runtime.CompilerServices;

using fin.image.formats;
using fin.math;

using SixLabors.ImageSharp.PixelFormats;

namespace fin.image.io.pixel;

/// <summary>
///   Helper class for reading 4-bit luminance/alpha pixels.
/// </summary>
public sealed class Al13PixelReader : IPixelReader<La16> {
  public IImage<La16> CreateImage(int width, int height)
    => new La16Image(PixelFormat.AL13, width, height);

  public void Decode(ReadOnlySpan<byte> data, Span<La16> scan0, int offset) {
    var value = data[0];

    var upper = (byte) ((value >> 4) * 17);
    var lower = (byte) ((value & 0xF) * 17);

    scan0[offset + 0] = FromNibble_(upper);
    scan0[offset + 1] = FromNibble_(lower);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private static La16 FromNibble_(byte nibble)
    => new(BitLogic.Expand3To8((nibble >> 1) & 0x7),
           (byte) (nibble.GetBit(0) ? 255 : 0));

  public int PixelsPerRead => 2;
  public int BitsPerPixel => 4;
}