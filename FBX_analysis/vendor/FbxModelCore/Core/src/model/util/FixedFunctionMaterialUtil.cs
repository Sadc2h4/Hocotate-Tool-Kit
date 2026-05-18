using System;
using System.Linq;
using System.Numerics;

using fin.image;
using fin.image.formats;
using fin.image.util;
using fin.language.equations.fixedFunction;
using fin.math;

using SixLabors.ImageSharp.PixelFormats;

namespace fin.model.util;

public static class FixedFunctionMaterialUtil {
  public static bool TryToGetCompiledDiffuseImage(
      this IReadOnlyFixedFunctionMaterial material,
      out Rgba32Image compiledDiffuseImage) {
    var width = -1;
    var height = -1;

    var textureSources = material.TextureSources;
    foreach (var texture in textureSources) {
      if (texture == null) {
        continue;
      }

      var image = texture.Image;
      width = Math.Max(width, image.Width);
      height = Math.Max(height, image.Height);
    }

    if (width == -1 || height == -1) {
      compiledDiffuseImage = null!;
      return false;
    }

    var compiledImage = new Rgba32Image(PixelFormat.RGBA8888, width, height);

    var textureImages = textureSources.Select(t => t?.Image).ToArray();

    var isOpaque = material.GetTransparencyType() == TransparencyType.OPAQUE ||
                   material is {
                       ColorDstFactor: BlendFactor.ZERO,
                       ColorSrcFactor: BlendFactor.ONE
                   };

    AccessMultipleImages_(
        textureImages,
        accessHandlers => {
          var dstLock = compiledImage.Lock();
          var dstScan0 = dstLock.Pixels;

          var outputColor = material.Equations.ColorOutputs[
              FixedFunctionSource.OUTPUT_COLOR];
          var outputAlpha = material.Equations.ScalarOutputs[
              FixedFunctionSource.OUTPUT_ALPHA];

          try {
            for (var y = 0; y < height; ++y) {
              for (var x = 0; x < width; ++x) {
                var rgb = EvaluateColor_(outputColor,
                                         1f * x / width,
                                         1f * y / height,
                                         textureImages,
                                         accessHandlers) * 255;
                var a = isOpaque
                    ? 255
                    : EvaluateScalar_(outputAlpha,
                                      1f * x / width,
                                      1f * y / height,
                                      textureImages,
                                      accessHandlers) * 255;

                dstScan0[y * width + x] = new Rgba32(
                    (byte) rgb.X.Clamp(0, 255),
                    (byte) rgb.Y.Clamp(0, 255),
                    (byte) rgb.Z.Clamp(0, 255),
                    (byte) a.Clamp(0, 255));
              }
            }
          } catch (Exception e) {
            compiledImage.Dispose();
            compiledImage = null;
          }
        });

    compiledDiffuseImage = compiledImage;
    return compiledImage != null;
  }

  private static void AccessMultipleImages_(
      IReadOnlyImage?[] images,
      Action<IImage.Rgba32GetHandler[]> handler) {
    var accessHandlers = new IImage.Rgba32GetHandler?[images.Length];
    AccessMultipleImagesImpl_(images, accessHandlers, 0, handler);
  }

  private static void AccessMultipleImagesImpl_(
      IReadOnlyImage?[] images,
      IImage.Rgba32GetHandler?[] accessHandlers,
      int i,
      Action<IImage.Rgba32GetHandler[]> handler) {
    if (i == images.Length) {
      handler(accessHandlers);
      return;
    }

    var image = images[i];
    if (image != null) {
      image.Access(accessHandler => {
        accessHandlers[i] = accessHandler;
        AccessMultipleImagesImpl_(images, accessHandlers, i + 1, handler);
      });
    } else {
      AccessMultipleImagesImpl_(images, accessHandlers, i + 1, handler);
    }
  }

  private static Vector3 EvaluateColor_(
      IColorValue colorValue,
      float xFraction,
      float yFraction,
      IReadOnlyImage?[] images,
      IImage.Rgba32GetHandler?[] accessHandlers) {
    var value = EvaluateColorImpl_(colorValue,
                                   xFraction,
                                   yFraction,
                                   images,
                                   accessHandlers);

    if (colorValue.Clamp) {
      value = new Vector3(value.X.Clamp(0, 1),
                          value.Y.Clamp(0, 1),
                          value.Z.Clamp(0, 1));
    }

    return value;
  }

  private static Vector3 EvaluateColorImpl_(
      IColorValue colorValue,
      float xFraction,
      float yFraction,
      IReadOnlyImage?[] images,
      IImage.Rgba32GetHandler?[] accessHandlers) {
    switch (colorValue) {
      case IColorOutput<FixedFunctionSource> colorOutput: {
        return EvaluateColor_(colorOutput.ColorValue,
                              xFraction,
                              yFraction,
                              images,
                              accessHandlers);
      }
      case IColorConstant colorConstant: {
        return colorConstant.IntensityValue != null
            ? new Vector3(colorConstant.IntensityValue.Value)
            : new Vector3(colorConstant.RValue,
                          colorConstant.GValue,
                          colorConstant.BValue);
      }
      case ColorWrapper colorWrapper: {
        return new Vector3(EvaluateScalar_(colorWrapper.R,
                                           xFraction,
                                           yFraction,
                                           images,
                                           accessHandlers),
                           EvaluateScalar_(colorWrapper.G,
                                           xFraction,
                                           yFraction,
                                           images,
                                           accessHandlers),
                           EvaluateScalar_(colorWrapper.B,
                                           xFraction,
                                           yFraction,
                                           images,
                                           accessHandlers));
      }
      case IColorExpression colorExpression: {
        var total = Vector3.Zero;
        foreach (var term in colorExpression.Terms) {
          total += EvaluateColor_(term,
                                  xFraction,
                                  yFraction,
                                  images,
                                  accessHandlers);
        }

        return total;
      }
      case IColorTerm colorTerm: {
        var numerator = Vector3.One;
        var denominator = Vector3.One;

        foreach (var numeratorFactor in colorTerm.NumeratorFactors) {
          numerator *= EvaluateColor_(numeratorFactor,
                                      xFraction,
                                      yFraction,
                                      images,
                                      accessHandlers);
        }

        if (colorTerm.DenominatorFactors != null) {
          foreach (var denominatorFactor in colorTerm.DenominatorFactors) {
            denominator *= EvaluateColor_(denominatorFactor,
                                          xFraction,
                                          yFraction,
                                          images,
                                          accessHandlers);
          }
        }

        return numerator / denominator;
      }
      case IColorInput<FixedFunctionSource> colorInput: {
        var identifier = colorInput.Identifier;
        if (identifier is (>= FixedFunctionSource.LIGHT_DIFFUSE_ALPHA_0
                           and <= FixedFunctionSource.LIGHT_DIFFUSE_ALPHA_7)
                          or (>= FixedFunctionSource.LIGHT_DIFFUSE_COLOR_0
                              and <= FixedFunctionSource.LIGHT_DIFFUSE_COLOR_7)
                          or FixedFunctionSource.LIGHT_DIFFUSE_ALPHA_MERGED
                          or FixedFunctionSource.LIGHT_DIFFUSE_COLOR_MERGED) {
          return Vector3.One;
        }

        if (identifier is (>= FixedFunctionSource.LIGHT_SPECULAR_ALPHA_0
                           and <= FixedFunctionSource.LIGHT_SPECULAR_ALPHA_7)
                          or (>= FixedFunctionSource.LIGHT_SPECULAR_COLOR_0
                              and <= FixedFunctionSource.LIGHT_SPECULAR_COLOR_7)
                          or FixedFunctionSource.LIGHT_SPECULAR_ALPHA_MERGED
                          or FixedFunctionSource.LIGHT_SPECULAR_COLOR_MERGED) {
          return Vector3.Zero;
        }

        if (identifier is >= FixedFunctionSource.TEXTURE_COLOR_0
                          and <= FixedFunctionSource.TEXTURE_COLOR_7) {
          var textureI = identifier - FixedFunctionSource.TEXTURE_COLOR_0;

          var image = images[textureI];

          var x = (byte) (xFraction * image.Width);
          var y = (byte) (yFraction * image.Height);

          accessHandlers[textureI](x, y, out var r, out var g, out var b, out _);

          return new Vector3(r, g, b) / 255f;
        }

        if (identifier is >= FixedFunctionSource.TEXTURE_ALPHA_0
                          and <= FixedFunctionSource.TEXTURE_ALPHA_7) {
          var textureI = identifier - FixedFunctionSource.TEXTURE_ALPHA_0;

          var image = images[textureI];

          var x = (byte) (xFraction * image.Width);
          var y = (byte) (yFraction * image.Height);

          accessHandlers[textureI](x, y, out _, out _, out _, out var a);

          return new Vector3(a) / 255f;
        }

        throw new NotImplementedException();
      }
      default: throw new NotImplementedException();
    }
  }

  private static float EvaluateScalar_(
      IScalarValue scalarValue,
      float xFraction,
      float yFraction,
      IReadOnlyImage?[] images,
      IImage.Rgba32GetHandler?[] accessHandlers) {
    var value = EvaluateScalarImpl_(scalarValue,
                                    xFraction,
                                    yFraction,
                                    images,
                                    accessHandlers);

    if (scalarValue.Clamp) {
      value = value.Clamp(0, 1);
    }

    return value;
  }

  private static float EvaluateScalarImpl_(
      IScalarValue scalarValue,
      float xFraction,
      float yFraction,
      IReadOnlyImage?[] images,
      IImage.Rgba32GetHandler?[] accessHandlers) {
    switch (scalarValue) {
      case IScalarOutput<FixedFunctionSource> scalarOutput: {
        return EvaluateScalar_(scalarOutput.ScalarValue,
                               xFraction,
                               yFraction,
                               images,
                               accessHandlers);
      }
      case IScalarConstant scalarConstant: {
        return scalarConstant.Value;
      }
      case IScalarExpression scalarExpression: {
        var total = 0f;
        foreach (var term in scalarExpression.Terms) {
          total += EvaluateScalar_(term,
                                   xFraction,
                                   yFraction,
                                   images,
                                   accessHandlers);
        }

        return total;
      }
      case IScalarTerm scalarTerm: {
        var numerator = 1f;
        var denominator = 1f;

        foreach (var numeratorFactor in scalarTerm.NumeratorFactors) {
          numerator *= EvaluateScalar_(numeratorFactor,
                                       xFraction,
                                       yFraction,
                                       images,
                                       accessHandlers);
        }

        if (scalarTerm.DenominatorFactors != null) {
          foreach (var denominatorFactor in scalarTerm.DenominatorFactors) {
            denominator *= EvaluateScalar_(denominatorFactor,
                                           xFraction,
                                           yFraction,
                                           images,
                                           accessHandlers);
          }
        }

        return numerator / denominator;
      }
      case IColorValueSwizzle colorValueSwizzle: {
        var color = EvaluateColor_(colorValueSwizzle.Source,
                                   xFraction,
                                   yFraction,
                                   images,
                                   accessHandlers);

        return colorValueSwizzle.SwizzleType switch {
            ColorSwizzle.R => color.X,
            ColorSwizzle.G => color.Y,
            ColorSwizzle.B => color.Z,
            _              => throw new ArgumentOutOfRangeException()
        };
      }
      case IColorNamedValueSwizzle<FixedFunctionSource> colorNamedValueSwizzle
          : {
        var color = EvaluateColor_(colorNamedValueSwizzle.Source,
                                   xFraction,
                                   yFraction,
                                   images,
                                   accessHandlers);

        return colorNamedValueSwizzle.SwizzleType switch {
            ColorSwizzle.R => color.X,
            ColorSwizzle.G => color.Y,
            ColorSwizzle.B => color.Z,
            _              => throw new ArgumentOutOfRangeException()
        };
      }
      case IScalarInput<FixedFunctionSource> scalarInput: {
        var identifier = scalarInput.Identifier;
        if (identifier is (>= FixedFunctionSource.LIGHT_DIFFUSE_ALPHA_0
                           and <= FixedFunctionSource.LIGHT_DIFFUSE_ALPHA_7)
                          or FixedFunctionSource.LIGHT_DIFFUSE_ALPHA_MERGED) {
          return 1;
        }

        if (identifier is (>= FixedFunctionSource.LIGHT_SPECULAR_ALPHA_0
                           and <= FixedFunctionSource.LIGHT_SPECULAR_ALPHA_7)
                          or FixedFunctionSource.LIGHT_SPECULAR_ALPHA_MERGED) {
          return 0;
        }

        if (identifier is >= FixedFunctionSource.TEXTURE_ALPHA_0
                          and <= FixedFunctionSource.TEXTURE_ALPHA_7) {
          var textureI = identifier - FixedFunctionSource.TEXTURE_ALPHA_0;

          var image = images[textureI];

          var x = (byte) (xFraction * image.Width);
          var y = (byte) (yFraction * image.Height);

          accessHandlers[textureI](x, y, out _, out _, out _, out var a);

          return a / 255f;
        }

        throw new NotImplementedException();
      }
      default: {
        throw new NotImplementedException();
      }
    }
  }
}