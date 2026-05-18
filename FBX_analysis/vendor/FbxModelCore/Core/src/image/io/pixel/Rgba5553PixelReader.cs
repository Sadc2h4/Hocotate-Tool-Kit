using System;

using CommunityToolkit.HighPerformance;

using fin.image.formats;
using fin.math;
using fin.color;

using SixLabors.ImageSharp.PixelFormats;

namespace fin.image.io.pixel;

public sealed class Rgba5553PixelReader : IPixelReader<Rgba32> {
  public IImage<Rgba32> CreateImage(int width, int height)
    => new Rgba32Image(PixelFormat.RGBA5553, width, height);

  public void Decode(ReadOnlySpan<byte> data, Span<Rgba32> scan0, int offset) {
    var pix = data.Cast<byte, ushort>()[0];
    ColorUtil.SplitRgb5A3(pix, out var r, out var g, out var b, out var a);
    scan0[offset] = new Rgba32(r, g, b, a);
  }

  public int BitsPerPixel => 16;
}