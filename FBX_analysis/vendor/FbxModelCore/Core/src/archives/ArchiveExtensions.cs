using System.IO;

using fin.io;
using fin.io.archive;

namespace fin.archives;

public static class ArchiveExtensions {
  public static ArchiveExtractionResult TryToImportAndExtractLocally<TBundle>(
      this IArchiveImporter<TBundle> importer,
      TBundle bundle,
      bool cleanUp = false) where TBundle : IArchiveBundle {
    var directory = new FinDirectory(bundle.MainFile.FullNameWithoutExtension);
    if (directory is { Exists: true, IsEmpty: false }) {
      return ArchiveExtractionResult.ALREADY_EXISTS;
    }

    importer.ImportAndExtractRelativeTo(bundle, directory, cleanUp);

    return ArchiveExtractionResult.NEWLY_EXTRACTED;
  }
}