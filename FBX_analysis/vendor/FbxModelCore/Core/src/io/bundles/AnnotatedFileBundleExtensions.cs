namespace fin.io.bundles;

public static class AnnotatedFileBundleExtensions {
  public static bool IsOfType<TSpecificFile>(
      this IFileBundle file,
      out TSpecificFile outFile)
      where TSpecificFile : IFileBundle {
    if (file is TSpecificFile bundle) {
      outFile = bundle;
      return true;
    }

    outFile = default!;
    return false;
  }
}