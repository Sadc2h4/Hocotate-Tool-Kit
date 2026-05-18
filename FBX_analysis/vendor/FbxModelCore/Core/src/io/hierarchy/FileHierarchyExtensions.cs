using System.Collections.Generic;
using System.Linq;

namespace fin.io.hierarchy;

public static class FileHierarchyExtensions {
  // TODO: Ideally clean this up and use the IReadOnlyTreeDirectory version
  public static IEnumerable<IFileHierarchyFile> GetExistingFilesRecursive(
      this IFileHierarchyDirectory treeDirectory)
    => treeDirectory
       .GetExistingFiles()
       .Concat(treeDirectory
               .GetExistingSubdirs()
               .SelectMany(d => d.GetExistingFilesRecursive()));
}