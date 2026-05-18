using System.Collections.Generic;

using fin.data.lazy;

namespace fin.io.bundles;

public interface IFileBundleOrganizer {
  void Add(IFileBundle annotatedFileBundle);
}

public sealed class FileBundleTreeOrganizer : IFileBundleOrganizer {
  private readonly FileBundleDirectory root_ = new("(root)");

  private readonly
      LazyDictionary<IReadOnlyTreeDirectory, IFileBundleDirectory>
      lazyDirToBundleDir_;

  public FileBundleTreeOrganizer() {
    this.lazyDirToBundleDir_ = new((lazyDict, dir) => {
      if (!dir.TryGetParent(out var parent)) {
        return this.root_.AddSubdir(dir);
      }

      return lazyDict[parent].AddSubdir(dir);
    });
  }

  public void Add(IFileBundle annotatedFileBundle) {
    this.lazyDirToBundleDir_[annotatedFileBundle.MainFile.AssertGetParent()]
        .AddFileBundle(annotatedFileBundle);
  }

  public IFileBundleDirectory CleanUpAndGetRoot() {
    this.root_.CleanUp();
    return this.root_;
  }
}

public sealed class FileBundleListOrganizer : IFileBundleOrganizer {
  public List<IFileBundle> List { get; } = [];

  public void Add(IFileBundle annotatedFileBundle)
    => this.List.Add(annotatedFileBundle);
}