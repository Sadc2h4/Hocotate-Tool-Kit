using fin.image.formats;

using SixLabors.ImageSharp.PixelFormats;

using Color = System.Drawing.Color;
using Rectangle = System.Drawing.Rectangle;

namespace fin.image;

public static class ImageExtensions {
  public static IImage RemoveTopLeftBackgroundColor(this IReadOnlyImage src) {
    Color topLeftBackgroundColor = default;
    src.Access(getHandler => {
      getHandler(0, 0, out var r, out var g, out var b, out _);
      topLeftBackgroundColor = Color.FromArgb(r, g, b);
    });
    return src.RemoveBackgroundColor(topLeftBackgroundColor);
  }

  public static unsafe IImage RemoveBackgroundColor(this IReadOnlyImage src,
                                                    Color color) {
    var width = src.Width;
    var height = src.Height;

    var textureImageWithAlpha = new Rgba32Image(src.PixelFormat, width, height);
    using var alphaLock = textureImageWithAlpha.UnsafeLock();
    var alphaScan0 = alphaLock.pixelScan0;

    src.Access(getHandler => {
      for (var y = 0; y < height; ++y) {
        for (var x = 0; x < width; ++x) {
          getHandler(x,
                     y,
                     out var r,
                     out var g,
                     out var b,
                     out var a);

          if (r == color.R && g == color.G && b == color.B) {
            a = 0;
          }

          alphaScan0[y * width + x] = new Rgba32(r, g, b, a);
        }
      }
    });

    return textureImageWithAlpha;
  }

  public static IImage SubImage(this IReadOnlyImage src, Rectangle region) {
    var dst = new Rgba32Image(src.PixelFormat, region.Width, region.Height);

    var dstLock = dst.Lock();

    src.Access(getHandler => {
      for (var dstY = 0; dstY < dst.Height; ++dstY) {
        var srcY = region.Y + dstY;

        for (var dstX = 0; dstX < dst.Width; ++dstX) {
          var srcX = region.X + dstX;

          getHandler(srcX, srcY, out var r, out var g, out var b, out var a);

          var dstI = dstY * dst.Width + dstX;
          dstLock.Pixels[dstI] = new Rgba32(r, g, b, a);
        }
      }
    });

    return dst;
  }
}