namespace fin.config;

public static class FinConfig {
  public static bool CacheFileHierarchies { get; set; }
  public static bool CleanUpArchives { get; set; }
  public static bool PreferGlNativeInterop { get; set; } = true;
  public static bool ShowSkeleton { get; set; }
  public static bool VerifyCachedFileHierarchySize { get; set; }
}