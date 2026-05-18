using System;

namespace fin.util.gc;

public static class GcUtil {
  public static void ForceCollectEverything(bool trackUsage = false) {
    var memoryBefore = GetMemoryUsedByProgram();
    GC.Collect();
    GC.WaitForFullGCComplete();
    GC.WaitForPendingFinalizers();
    var memoryAfter = GetMemoryUsedByProgram();

    var delta = memoryAfter - memoryBefore;
    if (trackUsage) {
      ;
    }
  }

  public static long GetMemoryUsedByProgram()
    => GC.GetTotalMemory(false);
}