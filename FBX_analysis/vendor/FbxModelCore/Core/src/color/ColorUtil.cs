using System.Runtime.CompilerServices;

using fin.math;

namespace fin.color;

public static class ColorUtil {
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static byte ExtractScaled(ushort col, int offset, int count) {
    var maxPossible = 1 << count;
    var factor = 255f / maxPossible;
    return ExtractScaled(col, offset, count, factor);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static byte ExtractScaled(
      ushort col,
      int offset,
      int count,
      float factor) {
    var extracted = BitLogic.ExtractFromRight(col, offset, count);
    return (byte) (extracted * factor);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static void SplitRgb565(
      ushort color,
      out byte r,
      out byte g,
      out byte b) {
    r = BitLogic.Expand5To8((color >> 11) & 0x1F);
    g = (byte) ((color >> 3) & 0b11111100);
    b = BitLogic.Expand5To8(color & 0x1F);
  }

  public static IColor ParseRgb565(ushort color) {
    SplitRgb565(color, out var r, out var g, out var b);
    return FinColor.FromRgbBytes(r, g, b);
  }

  public static void SplitRgb5A3(
      ushort color,
      out byte r,
      out byte g,
      out byte b,
      out byte a) {
    var alphaFlag = color.GetBit(15);

    if (alphaFlag) {
      a = 255;
      r = BitLogic.Expand5To8((color >> 10) & 0x1F);
      g = BitLogic.Expand5To8((color >> 5) & 0x1F);
      b = BitLogic.Expand5To8(color & 0x1F);
    } else {
      a = BitLogic.Expand3To8((color >> 12) & 0x7);
      r = BitLogic.Expand4To8((color >> 8) & 0xF);
      g = BitLogic.Expand4To8((color >> 4) & 0xF);
      b = BitLogic.Expand4To8(color & 0xF);
    }
  }

  public static IColor ParseRgb5A3(ushort color) {
    SplitRgb5A3(color, out var r, out var g, out var b, out var a);
    return FinColor.FromRgbaBytes(r, g, b, a);
  }

  public static void SplitRgb5A1(
      ushort color,
      out byte r,
      out byte g,
      out byte b,
      out byte a) {
    a = (byte) (color.GetBit(15) ? 255 : 0);
    r = BitLogic.Expand5To8((color >> 10) & 0x1F);
    g = BitLogic.Expand5To8((color >> 5) & 0x1F);
    b = BitLogic.Expand5To8(color & 0x1F);
  }

  public static IColor ParseRgb5A1(ushort color) {
    SplitRgb5A1(color, out var r, out var g, out var b, out var a);
    return FinColor.FromRgbaBytes(r, g, b, a);
  }

  public static IColor ParseBgr5A1(ushort color) {
    SplitRgb5A1(color, out var b, out var g, out var r, out var a);
    return FinColor.FromRgbaBytes(r, g, b, a);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static void SplitArgb1555(
      ushort color,
      out byte r,
      out byte g,
      out byte b,
      out byte a) {
    a = (byte) ((color & 1) == 1 ? 255 : 0);
    r = BitLogic.Expand5To8((color >> 11) & 0x1F);
    g = BitLogic.Expand5To8((color >> 6) & 0x1F);
    b = BitLogic.Expand5To8((color >> 1) & 0x1F);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static void SplitRgba4444(
      ushort color,
      out byte r,
      out byte g,
      out byte b,
      out byte a) {
    r = BitLogic.Expand4To8((color >> 12) & 0xF);
    g = BitLogic.Expand4To8((color >> 8) & 0xF);
    b = BitLogic.Expand4To8((color >> 4) & 0xF);
    a = BitLogic.Expand4To8(color & 0xF);
  }
}