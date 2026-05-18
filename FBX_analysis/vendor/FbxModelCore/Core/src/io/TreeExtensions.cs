using System.Collections.Generic;
using System.Linq;

using fin.util.strings;

namespace fin.io;

public static class TreeExtensions {
  public static string AssertGetPathRelativeTo<TIoObject, TDirectory, TFile,
                                               TFileType>(
      this IReadOnlyTreeIoObject<TIoObject, TDirectory, TFile, TFileType>
          treeIoObject,
      IReadOnlyTreeDirectory<TIoObject, TDirectory, TFile, TFileType> parent)
      where TIoObject :
      IReadOnlyTreeIoObject<TIoObject, TDirectory, TFile, TFileType>
      where TDirectory :
      IReadOnlyTreeDirectory<TIoObject, TDirectory, TFile, TFileType>
      where TFile : IReadOnlyTreeFile<TIoObject, TDirectory, TFile,
          TFileType>
    => treeIoObject.FullPath.AssertRemoveStart(parent.FullPath);

  public static IEnumerable<TFile> GetExistingFilesRecursive<
      TIoObject, TDirectory, TFile, TFileType>(
      this IReadOnlyTreeDirectory<TIoObject, TDirectory, TFile, TFileType>
          treeDirectory)
      where TIoObject :
      IReadOnlyTreeIoObject<TIoObject, TDirectory, TFile, TFileType>
      where TDirectory :
      IReadOnlyTreeDirectory<TIoObject, TDirectory, TFile, TFileType>
      where TFile : IReadOnlyTreeFile<TIoObject, TDirectory, TFile,
          TFileType>
    => treeDirectory
       .GetExistingFiles()
       .Concat(treeDirectory
               .GetExistingSubdirs()
               .SelectMany(d => d.GetExistingFilesRecursive()));
}