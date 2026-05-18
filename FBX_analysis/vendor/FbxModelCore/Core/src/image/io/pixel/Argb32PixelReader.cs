using System;

using CommunityToolkit.HighPerformance;

using fin.color;
using fin.image.formats;

using SixLabors.ImageSharp.PixelFormats;


namespace fin.image.io.pixel;

/// <summary>
///   Helper class for reading 32-bit RGBA pixels.
/// </summary>
public sealed class Argb32PixelReader : IPixelReader<Rgba32> {
  public IImage<Rgba32> CreateImage(int width, int height)
    => new Rgba32Image(PixelFormat.RGBA8888, width, height);

  public void Decode(ReadOnlySpan<byte> data, Span<Rgba32> scan0, int offset) {
    var value = data.Cast<byte, uint>()[0];

    var r = (byte) (value >> 24);
    var g = (byte) (value >> 16);
    var b = (byte) (value >> 8);
    var a = (byte) value;

    scan0[offset] = new Rgba32(r, g, b, a);
  }

  public int BitsPerPixel => 32;
}