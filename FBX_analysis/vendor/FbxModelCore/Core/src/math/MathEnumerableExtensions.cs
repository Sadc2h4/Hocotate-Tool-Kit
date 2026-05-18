using System.Collections.Generic;
using System.Linq;

using fin.util.linq;


namespace fin.math;

public static class MathEnumerableExtensions {
  public static bool IsSequentiallyIncreasing(
      this IEnumerable<int> values,
      out int start,
      out int count) {
    if (!values.TryGetFirst(out start)) {
      count = 0;
      return false;
    }

    var previous = start;

    count = 1;
    foreach (var value in values.Skip(1)) {
      if (value != previous + 1) {
        return false;
      }

      count++;
      previous = value;
    }

    return true;
  }
}