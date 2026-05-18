using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace fin.util.strings;

/// <summary>
///   Shamelessly stolen from:
///   https://github.com/pstlnce/indent-writer/blob/1d4743bab04b1f547f06c4625b58448c2ae46b93/IndentWriter/IndentedInterpolatedStringHandler.cs
/// </summary>
[InterpolatedStringHandler]
public readonly struct IndentedInterpolatedStringHandler {
  private readonly IndentedStringBuilder sb_;

  public IndentedInterpolatedStringHandler(int literalLength,
                                           int formattedCount,
                                           IndentedStringBuilder sb)
    => this.sb_ = sb;

  public void AppendLiteral(ReadOnlySpan<char> literal)
    => this.sb_.Append(literal);

  public void AppendFormatted<T>(T value) {
    // It has already added data to targeted StringBuilder
    // just used for visualization
    if (typeof(IndentedInterpolatedStringHandler) == typeof(T)) {
      return;
    }

    if (value is string str) {
      this.sb_.Append(str);
      return;
    }

    if (value is not IEnumerable<string> strings) {
      if (value is not null) {
        this.sb_.Append(value.ToString()!);
      }

      return;
    }

    foreach (var item in strings) {
      this.sb_.Append(item);
    }
  }

  public void AppendFormatted<T>(T value, string format)
      where T : IFormattable {
    this.sb_.Append(value.ToString(format, null));
  }
}