using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using fin.config;
using fin.data.stacks;
using fin.io.sharpDirLister;
using fin.util.asserts;
using fin.util.lists;

using schema.binary;

namespace fin.io.hierarchy;

public partial class CachedFileHierarchy : IFileHierarchy {
  private readonly ISystemDirectory directory_;
  private readonly ISystemFile cacheFile_;

  private readonly FinRootDirectory finRootDirectory_;

  public CachedFileHierarchy(string name,
                             ISystemDirectory directory,
                             ISystemFile cacheFile,
                             bool forceUseCaching = false) {
    this.Name = name;
    this.directory_ = directory;
    this.cacheFile_ = cacheFile;

    SchemaDirectoryInformation? populatedSubdirs = null;
    var useCaching = forceUseCaching || FinConfig.CacheFileHierarchies;
    if (useCaching) {
      long? actualSize = FinConfig.VerifyCachedFileHierarchySize
          ? FinDirectoryStatic.GetTotalSize(directory.FullPath)
          : null;
      if (cacheFile.Exists) {
        var header = cacheFile.ReadNew<CachedFileHierarchyDataHeader>();
        if (header.Version == CachedFileHierarchyDataHeader.CURRENT_VERSION &&
            (!FinConfig.VerifyCachedFileHierarchySize ||
             header.Size == actualSize)) {
          try {
            var data = cacheFile.ReadNew<CachedFileHierarchyData>();
            populatedSubdirs = data.Root;
          } catch {
            // TODO: Log error
          }
        }
      }

      populatedSubdirs ??= this.UpdateCacheFile_(actualSize);
    }

    populatedSubdirs ??= GetInfo_(directory);

    this.finRootDirectory_ = new FinRootDirectory(name, directory);
    this.Root = new FileHierarchyDirectory(this, populatedSubdirs);
  }

  private static SchemaDirectoryInformation GetInfo_(
      IReadOnlyTreeDirectory directory)
    => SchemaSharpFileLister.FindNextFilePInvoke(directory.FullPath, "");

  private SchemaDirectoryInformation UpdateCacheFile_(
      long? actualSize = null) {
    actualSize
        ??= FinDirectoryStatic.GetTotalSize(this.directory_.FullPath);
    var populatedSubdirs = GetInfo_(this.directory_);

    var data = new CachedFileHierarchyData {
        Header = new CachedFileHierarchyDataHeader {
            Size = actualSize.Value
        },
        Root = populatedSubdirs
    };
    this.cacheFile_.Write(data);

    return populatedSubdirs;
  }

  public string Name { get; }
  public IFileHierarchyDirectory Root { get; }

  public void RefreshRootAndUpdateCache() {
    this.Root.Refresh(true);

    if (FinConfig.CacheFileHierarchies) {
      this.UpdateCacheFile_();
    }
  }

  [BinarySchema]
  private sealed partial class CachedFileHierarchyDataHeader : IBinaryConvertible {
    public const uint CURRENT_VERSION = 1;
    public uint Version { get; set; } = CURRENT_VERSION;
    public long Size { get; set; }
  }

  [BinarySchema]
  private sealed partial class CachedFileHierarchyData : IBinaryConvertible {
    public CachedFileHierarchyDataHeader Header { get; set; } = new();
    public SchemaDirectoryInformation Root { get; set; } = new();
  }

  private abstract class BFileHierarchyIoObject : IFileHierarchyIoObject {
    protected BFileHierarchyIoObject(CachedFileHierarchy hierarchy) {
      this.TypedHierarchy = hierarchy;
      this.LocalPath = string.Empty;
    }

    protected BFileHierarchyIoObject(
        CachedFileHierarchy hierarchy,
        IFileHierarchyDirectory parent,
        ISystemIoObject instance) {
      this.TypedHierarchy = hierarchy;
      this.Parent = parent;

      this.LocalPath = instance.FullPath[
              hierarchy.finRootDirectory_.Impl.FullPath.Length..];
    }

    protected abstract ISystemIoObject Instance { get; }

    public string LocalPath { get; }
    public IFileHierarchy Hierarchy => this.TypedHierarchy;
    protected CachedFileHierarchy TypedHierarchy { get; }
    public IFileHierarchyDirectory? Parent { get; }

    public IReadOnlyTreeDirectory AssertGetParent()
      => Asserts.True(this.TryGetParent(out var parent))
          ? parent
          : null!;

    public bool TryGetParent(out IReadOnlyTreeDirectory parent) {
      parent = this.Parent!;
      return this.Parent != null;
    }

    public IEnumerable<IReadOnlyTreeDirectory> GetAncestry()
      => this.Instance.GetAncestry();

    public bool Exists => true;
    public string FullPath => this.Instance.FullPath;

    public ReadOnlySpan<char> Name => this.Parent == null
        ? this.Hierarchy.Name
        : this.Instance.Name;

    public override string ToString() => this.LocalPath;
  }

  private sealed class FileHierarchyDirectory
      : BFileHierarchyIoObject,
        IFileHierarchyDirectory {
    private readonly List<IFileHierarchyDirectory> subdirs_ = [];
    private readonly List<IFileHierarchyFile> files_ = [];

    public FileHierarchyDirectory(
        CachedFileHierarchy hierarchy,
        SchemaDirectoryInformation paths) : base(hierarchy) {
      this.Impl = hierarchy.finRootDirectory_.Impl;

      foreach (var fileName in paths.FileNames) {
        var path = Path.Join(this.Impl.FullPath, fileName.Name);
        this.files_.Add(
            new FileHierarchyFile(hierarchy,
                                  this,
                                  new FinFile(path,
                                              hierarchy.finRootDirectory_)));
      }

      foreach (var subdir in paths.Subdirs) {
        var path = Path.Join(this.Impl.FullPath, subdir.Name);
        this.subdirs_.Add(new FileHierarchyDirectory(
                              hierarchy,
                              this,
                              new FinDirectory(path, hierarchy.finRootDirectory_),
                              subdir));
      }
    }

    private FileHierarchyDirectory(
        CachedFileHierarchy hierarchy,
        IFileHierarchyDirectory parent,
        ISystemDirectory directory,
        SchemaDirectoryInformation paths) : base(
        hierarchy,
        parent,
        directory) {
      this.Impl = directory;

      foreach (var fileName in paths.FileNames) {
        var path = Path.Join(directory.FullPath, fileName.Name);
        this.files_.Add(
            new FileHierarchyFile(hierarchy, this, new FinFile(path, hierarchy.finRootDirectory_)));
      }

      foreach (var subdir in paths.Subdirs) {
        var path = Path.Join(directory.FullPath, subdir.Name);
        this.subdirs_.Add(new FileHierarchyDirectory(
                              hierarchy,
                              this,
                              new FinDirectory(path, hierarchy.finRootDirectory_),
                              subdir));
      }
    }

    private FileHierarchyDirectory(
        CachedFileHierarchy hierarchy,
        IFileHierarchyDirectory parent,
        ISystemDirectory directory) :
        base(hierarchy, parent, directory) {
      this.Impl = directory;
      this.Refresh();
    }

    protected override ISystemIoObject Instance => this.Impl;
    public ISystemDirectory Impl { get; }

    public bool IsEmpty => this.subdirs_.Count > 0;

    public IEnumerable<IFileHierarchyDirectory> GetExistingSubdirs()
      => this.subdirs_;

    public IEnumerable<IFileHierarchyFile> GetExistingFiles()
      => this.files_;

    public void Refresh(bool recursive = false) {
      if (this.subdirs_.Count == 0) {
        foreach (var actualSubdir in this.Impl.GetExistingSubdirs()) {
          this.subdirs_.Add(new FileHierarchyDirectory(this.TypedHierarchy,
                              this,
                              actualSubdir));
        }
      } else {
        var actualSubdirs = this.Impl.GetExistingSubdirs().ToArray();
        if (!actualSubdirs.SequenceEqual(this.subdirs_.Select(d => d.Impl))) {
          ListUtil.RemoveWhere(this.subdirs_,
                               subdir => !actualSubdirs
                                   .Contains(subdir.Impl));
          foreach (var actualSubdir in actualSubdirs) {
            if (this.subdirs_.All(
                    subdir => !subdir.Impl.Equals(actualSubdir))) {
              this.subdirs_.Add(
                  new FileHierarchyDirectory(this.TypedHierarchy,
                                             this,
                                             actualSubdir));
            }
          }
        }
      }

      if (this.files_.Count == 0) {
        foreach (var actualFile in this.Impl.GetExistingFiles()) {
          this.files_.Add(new FileHierarchyFile(this.TypedHierarchy,
                                                this,
                                                actualFile));
        }
      } else {
        var actualFiles = this.Impl.GetExistingFiles().ToArray();
        if (!actualFiles.SequenceEqual(this.files_.Select(d => d.Impl))) {
          ListUtil.RemoveWhere(this.files_,
                               file => !actualFiles.Contains(file.Impl));
          foreach (var actualFile in actualFiles) {
            if (this.files_.All(file => !file.Impl.Equals(actualFile))) {
              this.files_.Add(new FileHierarchyFile(this.TypedHierarchy,
                                                    this,
                                                    actualFile));
            }
          }
        }
      }

      if (recursive) {
        foreach (var subdir in this.subdirs_) {
          subdir.Refresh(true);
        }
      }
    }

    public IFileHierarchyFile AssertGetExistingFile(
        ReadOnlySpan<char> relativePath) {
      if (!this.TryToGetExistingFile(relativePath, out var outFile)) {
        Asserts.Fail(
            $"Expected to find file '{relativePath}' in '{this.FullPath}'");
      }

      return outFile;
    }

    public bool TryToGetExistingFile(ReadOnlySpan<char> localPath,
                                     out IFileHierarchyFile outFile)
      => this.TryToGetExistingFileImpl_(localPath, out outFile);

    private bool TryToGetExistingFileImpl_(
        ReadOnlySpan<char> localPath,
        out IFileHierarchyFile outFile) {
      var numSlashes = localPath.Count('/') + localPath.Count('\\');

      IFileHierarchyDirectory parentDir = this;
      ReadOnlySpan<char> filePath = localPath;
      if (numSlashes > 0) {
        Span<Range> subdirRanges = stackalloc Range[1 + numSlashes];
        localPath.SplitAny(subdirRanges, this.pathSeparators_);

        var lastSubdirRange = subdirRanges[^1];
        var parentDirPath
            = localPath[..(lastSubdirRange.Start.Value - 1)];
        if (!this.TryToGetExistingSubdirImpl_(parentDirPath, out parentDir)) {
          outFile = null;
          return false;
        }

        filePath = localPath[lastSubdirRange];
      }

      foreach (var child in parentDir.GetExistingFiles()) {
        if (MemoryExtensions.Equals(child.Name,
                                    filePath,
                                    StringComparison.OrdinalIgnoreCase)) {
          outFile = child;
          return true;
        }
      }

      outFile = null;
      return false;
    }

    public IFileHierarchyDirectory AssertGetExistingSubdir(
        ReadOnlySpan<char> relativePath) {
      if (!this.TryToGetExistingSubdir(relativePath, out var outDir)) {
        Asserts.Fail(
            $"Expected to find subdir '{relativePath}' in '{this.FullPath}'");
      }

      return outDir;
    }

    public bool TryToGetExistingSubdir(
        ReadOnlySpan<char> localPath,
        out IFileHierarchyDirectory outDirectory)
      => this.TryToGetExistingSubdirImpl_(localPath, out outDirectory);

    private readonly char[] pathSeparators_ = ['/', '\\'];

    private bool TryToGetExistingSubdirImpl_(
        ReadOnlySpan<char> localPath,
        out IFileHierarchyDirectory outDirectory) {
      var numSlashes = localPath.Count('/') + localPath.Count('\\');
      Span<Range> subdirRanges = stackalloc Range[1 + numSlashes];
      localPath.SplitAny(subdirRanges, this.pathSeparators_);

      IFileHierarchyDirectory current = this;
      foreach (var subdirRange in subdirRanges) {
        var subdir = localPath[subdirRange];

        if (subdir.Length == 0) {
          continue;
        }

        if (subdir is "..") {
          current = Asserts.CastNonnull(current.Parent);
          continue;
        }

        var foundMatch = false;
        foreach (var child in current.GetExistingSubdirs()) {
          if (MemoryExtensions.Equals(child.Name,
                                      subdir,
                                      StringComparison.OrdinalIgnoreCase)) {
            foundMatch = true;
            current = child;
            break;
          }
        }

        if (!foundMatch) {
          outDirectory = null;
          return false;
        }
      }

      outDirectory = current;
      return true;
    }

    public bool TryToGetExistingFileWithFileType(
        string pathWithoutExtension,
        out IFileHierarchyFile outFile,
        params string[] fileTypes) {
      outFile = null;
      var subdirs = pathWithoutExtension.Split('/', '\\');

      IFileHierarchyDirectory parentDir;
      if (subdirs.Length == 1) {
        parentDir = this;
      } else {
        var parentDirPath = string.Join('/', subdirs.SkipLast(1));
        if (!this.TryToGetExistingSubdir(parentDirPath, out parentDir)) {
          return false;
        }
      }

      var match =
          parentDir.GetExistingFiles()
                   .FirstOrDefault(
                       file => file.NameWithoutExtension.SequenceEqual(
                                   subdirs.Last()) &&
                               fileTypes.Contains(file.FileType));
      outFile = match;
      return match != null;
    }

    public IEnumerable<IFileHierarchyFile> GetFilesWithNameRecursive(
        string name) {
      var stack = new FinStack<IFileHierarchyDirectory>(this);
      while (stack.TryPop(out var next)) {
        var match = next.GetExistingFiles()
                        .FirstOrDefault(
                            file => file.Name.Equals(
                                name,
                                StringComparison.OrdinalIgnoreCase));
        if (match != null) {
          yield return match;
        }

        stack.Push(next.GetExistingSubdirs());
      }
    }

    public IEnumerable<IFileHierarchyFile> GetFilesWithFileType(
        string fileType,
        bool includeSubdirs = false)
      => includeSubdirs
          ? this.FilesWithExtensionRecursive(fileType)
          : this.FilesWithExtension(fileType);

    public IEnumerable<IFileHierarchyFile> FilesWithExtension(
        string extension)
      => this.GetExistingFiles()
             .Where(file => file.FullPath.EndsWith(
                        extension,
                        StringComparison.OrdinalIgnoreCase));

    public IEnumerable<IFileHierarchyFile> FilesWithExtensions(
        IEnumerable<string> extensions)
      => this.GetExistingFiles()
             .Where(
                 file => extensions.Any(
                     ext => file.FullPath.EndsWith(
                         ext,
                         StringComparison.OrdinalIgnoreCase)));

    public IEnumerable<IFileHierarchyFile> FilesWithExtensions(
        string first,
        params string[] rest)
      => this.GetExistingFiles()
             .Where(file
                        => file.FullPath.EndsWith(
                               first,
                               StringComparison.OrdinalIgnoreCase) ||
                           rest.Any(
                               ext => file.FullPath.EndsWith(
                                   ext,
                                   StringComparison.OrdinalIgnoreCase)));

    public IEnumerable<IFileHierarchyFile> FilesWithExtensionRecursive(
        string extension)
      => this.FilesWithExtension(extension)
             .Concat(
                 this.GetExistingSubdirs()
                     .SelectMany(
                         subdir
                             => subdir
                                 .FilesWithExtensionRecursive(extension)));

    public IEnumerable<IFileHierarchyFile> FilesWithExtensionsRecursive(
        IEnumerable<string> extensions)
      => this.FilesWithExtensions(extensions)
             .Concat(
                 this.GetExistingSubdirs()
                     .SelectMany(
                         subdir
                             => subdir.FilesWithExtensionsRecursive(
                                 extensions)));

    public IEnumerable<IFileHierarchyFile> FilesWithExtensionsRecursive(
        string first,
        params string[] rest)
      => this.FilesWithExtensions(first, rest)
             .Concat(
                 this.GetExistingSubdirs()
                     .SelectMany(
                         subdir
                             => subdir
                                 .FilesWithExtensionsRecursive(
                                     first,
                                     rest)));
  }

  private sealed class FileHierarchyFile(
      CachedFileHierarchy hierarchy,
      FileHierarchyDirectory parent,
      ISystemFile file)
      : BFileHierarchyIoObject(hierarchy, parent, file),
        IFileHierarchyFile {
    protected override ISystemIoObject Instance => this.Impl;
    public ISystemFile Impl { get; } = file;

    // File fields
    public string FileType => this.Impl.FileType;

    public string FullNameWithoutExtension
      => this.Impl.FullNameWithoutExtension;

    public ReadOnlySpan<char> NameWithoutExtension
      => this.Impl.NameWithoutExtension;

    public string DisplayFullPath
      => $"//{this.Hierarchy.Name}{this.LocalPath.Replace('\\', '/')}";

    public bool HasRoot(out string rootName, out string localPath) {
      var root = this.TypedHierarchy.finRootDirectory_;
      rootName = root.Name;
      localPath = this.FullPath[root.Impl.FullPath.Length..];
      return true;
    }

    public Stream OpenRead() => this.Impl.OpenRead();
  }

  public IEnumerator<IFileHierarchyDirectory> GetEnumerator() {
    var directoryQueue = new Queue<IFileHierarchyDirectory>();
    directoryQueue.Enqueue(this.Root);
    while (directoryQueue.Count > 0) {
      var directory = directoryQueue.Dequeue();

      yield return directory;

      foreach (var subdir in directory.GetExistingSubdirs()) {
        directoryQueue.Enqueue(subdir);
      }
    }
  }

  IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}