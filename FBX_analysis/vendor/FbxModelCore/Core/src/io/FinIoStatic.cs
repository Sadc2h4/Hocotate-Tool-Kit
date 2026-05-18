using System;
using System.IO;
using System.Runtime.CompilerServices;


namespace fin.io;

public static class FinIoStatic {
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static ReadOnlySpan<char> GetName(ReadOnlySpan<char> fullName)
    => Path.GetFileName(NormalizePath_(fullName));

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static ReadOnlySpan<char> GetParentFullName(
      ReadOnlySpan<char> fullName)
    => Path.GetDirectoryName(NormalizePath_(fullName));

  private static ReadOnlySpan<char> NormalizePath_(
      ReadOnlySpan<char> fullName) {
    fullName = Path.TrimEndingDirectorySeparator(fullName);
    if (fullName.StartsWith("//")) {
      fullName = fullName[2..];
    }
    return fullName;
  }
}