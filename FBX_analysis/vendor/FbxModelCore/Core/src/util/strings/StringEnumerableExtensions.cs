using System.Collections.Generic;
using System.Text;

namespace fin.util.strings;

public static class StringEnumerableExtensions {
  public static string Join(
      this IEnumerable<string> strings,
      string separator) {
    var i = 0;
    var sb = new StringBuilder();
    foreach (var str in strings) {
      if (i++ > 0) {
        sb.Append(separator);
      }

      sb.Append(str);
    }

    return sb.ToString();
  }
}