using System;
using System.Collections.Generic;
using System.IO;

using fin.importers;
using fin.io;
using fin.io.bundles;

namespace fin.archives;

public interface IArchiveBundle : IFileBundle {
  FileBundleType IFileBundle.Type => FileBundleType.ARCHIVE;
}

public interface IArchiveSubFile {
  string FullPath { get; }
  Stream OpenRead();
}

public interface IArchive : IResource, IDisposable {
  IReadOnlyList<IArchiveSubFile> FileEntries { get; }
}

public interface IArchiveImporter<in TBundle> : IImporter<IArchive, TBundle>
    where TBundle : IArchiveBundle {
  void ImportAndExtractRelativeTo(
      TBundle bundle,
      ISystemDirectory directory,
      bool cleanUp = false);
}