using System.Runtime.Versioning;
using System.Security.AccessControl;

namespace System.IO.Abstractions.TestingHelpers;

/// <inheritdoc />
#if FEATURE_SERIALIZABLE
[Serializable]
#endif
public class ImaginaryDirectoryInfo : DirectoryInfoBase, IFileSystemAclSupport {
  private readonly IImaginaryFileDataAccessor imaginaryFileDataAccessor_;
  private string directoryPath;
  private string originalPath;
  private ImaginaryFileData cachedImaginaryFileData_;
  private bool refreshOnNextRead;

  /// <summary>
  /// Initializes a new instance of the <see cref="ImaginaryDirectoryInfo"/> class.
  /// </summary>
  /// <param name="imaginaryFileDataAccessor">The mock file data accessor.</param>
  /// <param name="directoryPath">The directory path.</param>
  /// <exception cref="ArgumentNullException">Thrown if <paramref name="imaginaryFileDataAccessor"/> or <paramref name="directoryPath"/> is <see langref="null"/>.</exception>
  public ImaginaryDirectoryInfo(
      IImaginaryFileDataAccessor imaginaryFileDataAccessor,
      string directoryPath) : base(
      imaginaryFileDataAccessor?.FileSystem) {
    this.imaginaryFileDataAccessor_ = imaginaryFileDataAccessor ??
                                      throw new ArgumentNullException(
                                          nameof(imaginaryFileDataAccessor));

    if (directoryPath == null) {
      throw new ArgumentNullException("path",
                                      StringResources.Manager.GetString(
                                          "VALUE_CANNOT_BE_NULL"));
    }

    if (directoryPath.Trim() == string.Empty) {
      throw CommonExceptions.PathIsNotOfALegalForm("path");
    }

    SetDirectoryPath(directoryPath);
    Refresh();
  }

#if FEATURE_CREATE_SYMBOLIC_LINK
  /// <inheritdoc />
  public override void CreateAsSymbolicLink(string pathToTarget) {
    FileSystem.Directory.CreateSymbolicLink(FullName, pathToTarget);
  }
#endif

  /// <inheritdoc />
  public override void Delete() {
    this.imaginaryFileDataAccessor_.Directory.Delete(directoryPath);
    refreshOnNextRead = true;
  }

  /// <inheritdoc />
  public override void Refresh() {
    var mockFileData = this.imaginaryFileDataAccessor_.GetFile(directoryPath) ??
                       ImaginaryFileData.NullObject;
    this.cachedImaginaryFileData_ = mockFileData.Clone();
  }

#if FEATURE_CREATE_SYMBOLIC_LINK
  /// <inheritdoc />
  public override IFileSystemInfo ResolveLinkTarget(bool returnFinalTarget) {
    return FileSystem.Directory.ResolveLinkTarget(FullName, returnFinalTarget);
  }
#endif

  /// <inheritdoc />
  public override FileAttributes Attributes {
    get { return GetMockFileDataForRead().Attributes; }
    set {
      GetMockFileDataForWrite().Attributes = value | FileAttributes.Directory;
    }
  }

  /// <inheritdoc />
  public override DateTime CreationTime {
    get { return GetMockFileDataForRead().CreationTime.LocalDateTime; }
    set { GetMockFileDataForWrite().CreationTime = value; }
  }

  /// <inheritdoc />
  public override DateTime CreationTimeUtc {
    get { return GetMockFileDataForRead().CreationTime.UtcDateTime; }
    set { GetMockFileDataForWrite().CreationTime = value; }
  }

  /// <inheritdoc />
  public override bool Exists {
    get {
      var mockFileData = GetMockFileDataForRead();
      return (int) mockFileData.Attributes != -1 && mockFileData.IsDirectory;
    }
  }

  /// <inheritdoc />
  public override string Extension {
    get {
      // System.IO.Path.GetExtension does only string manipulation,
      // so it's safe to delegate.
      return Path.GetExtension(directoryPath);
    }
  }

  /// <inheritdoc />
  public override string FullName {
    get {
      var root
          = this.imaginaryFileDataAccessor_.Path.GetPathRoot(directoryPath);

      if (this.imaginaryFileDataAccessor_.StringOperations.Equals(
              directoryPath,
              root)) {
        // drives have the trailing slash
        return directoryPath;
      }

      // directories do not have a trailing slash
      return directoryPath.TrimEnd('\\').TrimEnd('/');
    }
  }

  /// <inheritdoc />
  public override DateTime LastAccessTime {
    get { return GetMockFileDataForRead().LastAccessTime.LocalDateTime; }
    set { GetMockFileDataForWrite().LastAccessTime = value; }
  }

  /// <inheritdoc />
  public override DateTime LastAccessTimeUtc {
    get { return GetMockFileDataForRead().LastAccessTime.UtcDateTime; }
    set { GetMockFileDataForWrite().LastAccessTime = value; }
  }

  /// <inheritdoc />
  public override DateTime LastWriteTime {
    get { return GetMockFileDataForRead().LastWriteTime.LocalDateTime; }
    set { GetMockFileDataForWrite().LastWriteTime = value; }
  }

  /// <inheritdoc />
  public override DateTime LastWriteTimeUtc {
    get { return GetMockFileDataForRead().LastWriteTime.UtcDateTime; }
    set { GetMockFileDataForWrite().LastWriteTime = value; }
  }

#if FEATURE_FILE_SYSTEM_INFO_LINK_TARGET
  /// <inheritdoc />
  public override string LinkTarget {
    get { return GetMockFileDataForRead().LinkTarget; }
  }
#endif

  /// <inheritdoc />
  public override string Name {
    get {
      var mockPath = new ImaginaryPath(this.imaginaryFileDataAccessor_);
      return string.Equals(mockPath.GetPathRoot(directoryPath), directoryPath)
          ? directoryPath
          : mockPath.GetFileName(directoryPath.TrimEnd(
                                     this.imaginaryFileDataAccessor_.Path
                                         .DirectorySeparatorChar));
    }
  }

  /// <inheritdoc />
  public override void Create() {
    this.imaginaryFileDataAccessor_.Directory.CreateDirectory(FullName);
    refreshOnNextRead = true;
  }

  /// <inheritdoc />
  public override IDirectoryInfo CreateSubdirectory(string path) {
    return this.imaginaryFileDataAccessor_.Directory.CreateDirectory(
        Path.Combine(FullName, path));
  }

  /// <inheritdoc />
  public override void Delete(bool recursive) {
    this.imaginaryFileDataAccessor_.Directory.Delete(directoryPath, recursive);
    refreshOnNextRead = true;
  }

  /// <inheritdoc />
  public override IEnumerable<IDirectoryInfo> EnumerateDirectories() {
    return GetDirectories();
  }

  /// <inheritdoc />
  public override IEnumerable<IDirectoryInfo> EnumerateDirectories(
      string searchPattern) {
    return GetDirectories(searchPattern);
  }

  /// <inheritdoc />
  public override IEnumerable<IDirectoryInfo> EnumerateDirectories(
      string searchPattern,
      SearchOption searchOption) {
    return GetDirectories(searchPattern, searchOption);
  }

#if FEATURE_ENUMERATION_OPTIONS
  /// <inheritdoc />
  public override IEnumerable<IDirectoryInfo> EnumerateDirectories(
      string searchPattern,
      EnumerationOptions enumerationOptions) {
    return GetDirectories(searchPattern, enumerationOptions);
  }
#endif

  /// <inheritdoc />
  public override IEnumerable<IFileInfo> EnumerateFiles() {
    return GetFiles();
  }

  /// <inheritdoc />
  public override IEnumerable<IFileInfo> EnumerateFiles(string searchPattern) {
    return GetFiles(searchPattern);
  }

  /// <inheritdoc />
  public override IEnumerable<IFileInfo> EnumerateFiles(
      string searchPattern,
      SearchOption searchOption) {
    return GetFiles(searchPattern, searchOption);
  }

#if FEATURE_ENUMERATION_OPTIONS
  /// <inheritdoc />
  public override IEnumerable<IFileInfo> EnumerateFiles(
      string searchPattern,
      EnumerationOptions enumerationOptions) {
    return GetFiles(searchPattern, enumerationOptions);
  }
#endif

  /// <inheritdoc />
  public override IEnumerable<IFileSystemInfo> EnumerateFileSystemInfos() {
    return GetFileSystemInfos();
  }

  /// <inheritdoc />
  public override IEnumerable<IFileSystemInfo> EnumerateFileSystemInfos(
      string searchPattern) {
    return GetFileSystemInfos(searchPattern);
  }

  /// <inheritdoc />
  public override IEnumerable<IFileSystemInfo> EnumerateFileSystemInfos(
      string searchPattern,
      SearchOption searchOption) {
    return GetFileSystemInfos(searchPattern, searchOption);
  }

#if FEATURE_ENUMERATION_OPTIONS
  /// <inheritdoc />
  public override IEnumerable<IFileSystemInfo> EnumerateFileSystemInfos(
      string searchPattern,
      EnumerationOptions enumerationOptions) {
    return GetFileSystemInfos(searchPattern, enumerationOptions);
  }
#endif

  /// <inheritdoc />
  public override IDirectoryInfo[] GetDirectories() {
    return ConvertStringsToDirectories(
        this.imaginaryFileDataAccessor_.Directory
            .GetDirectories(directoryPath));
  }

  /// <inheritdoc />
  public override IDirectoryInfo[] GetDirectories(string searchPattern) {
    return ConvertStringsToDirectories(
        this.imaginaryFileDataAccessor_.Directory.GetDirectories(
            directoryPath,
            searchPattern));
  }

  /// <inheritdoc />
  public override IDirectoryInfo[] GetDirectories(
      string searchPattern,
      SearchOption searchOption) {
    return ConvertStringsToDirectories(
        this.imaginaryFileDataAccessor_.Directory.GetDirectories(
            directoryPath,
            searchPattern,
            searchOption));
  }

#if FEATURE_ENUMERATION_OPTIONS
  /// <inheritdoc />
  public override IDirectoryInfo[] GetDirectories(
      string searchPattern,
      EnumerationOptions enumerationOptions) {
    return ConvertStringsToDirectories(
        imaginaryFileDataAccessor_.Directory.GetDirectories(
            directoryPath,
            searchPattern,
            enumerationOptions));
  }
#endif

  private DirectoryInfoBase[] ConvertStringsToDirectories(
      IEnumerable<string> paths) {
    return paths
           .Select(path => new ImaginaryDirectoryInfo(
                       this.imaginaryFileDataAccessor_,
                       path))
           .Cast<DirectoryInfoBase>()
           .ToArray();
  }

  /// <inheritdoc />
  public override IFileInfo[] GetFiles() {
    return ConvertStringsToFiles(
        this.imaginaryFileDataAccessor_.Directory.GetFiles(FullName));
  }

  /// <inheritdoc />
  public override IFileInfo[] GetFiles(string searchPattern) {
    return ConvertStringsToFiles(
        this.imaginaryFileDataAccessor_.Directory.GetFiles(
            FullName,
            searchPattern));
  }

  /// <inheritdoc />
  public override IFileInfo[] GetFiles(string searchPattern,
                                       SearchOption searchOption) {
    return ConvertStringsToFiles(
        this.imaginaryFileDataAccessor_.Directory.GetFiles(
            FullName,
            searchPattern,
            searchOption));
  }

#if FEATURE_ENUMERATION_OPTIONS
  /// <inheritdoc />
  public override IFileInfo[] GetFiles(string searchPattern,
                                       EnumerationOptions enumerationOptions) {
    return ConvertStringsToFiles(
        imaginaryFileDataAccessor_.Directory.GetFiles(
            FullName,
            searchPattern,
            enumerationOptions));
  }
#endif

  IFileInfo[] ConvertStringsToFiles(IEnumerable<string> paths) {
    return paths
           .Select(this.imaginaryFileDataAccessor_.FileInfo.New)
           .ToArray();
  }

  /// <inheritdoc />
  public override IFileSystemInfo[] GetFileSystemInfos() {
    return GetFileSystemInfos("*");
  }

  /// <inheritdoc />
  public override IFileSystemInfo[] GetFileSystemInfos(string searchPattern) {
    return GetFileSystemInfos(searchPattern, SearchOption.TopDirectoryOnly);
  }

  /// <inheritdoc />
  public override IFileSystemInfo[] GetFileSystemInfos(
      string searchPattern,
      SearchOption searchOption) {
    return GetDirectories(searchPattern, searchOption)
           .OfType<IFileSystemInfo>()
           .Concat(GetFiles(searchPattern, searchOption))
           .ToArray();
  }

#if FEATURE_ENUMERATION_OPTIONS
  /// <inheritdoc />
  public override IFileSystemInfo[] GetFileSystemInfos(
      string searchPattern,
      EnumerationOptions enumerationOptions) {
    return GetDirectories(searchPattern, enumerationOptions)
           .OfType<IFileSystemInfo>()
           .Concat(GetFiles(searchPattern, enumerationOptions))
           .ToArray();
  }
#endif

  /// <inheritdoc />
  public override void MoveTo(string destDirName) {
    this.imaginaryFileDataAccessor_.Directory.Move(directoryPath, destDirName);
    SetDirectoryPath(destDirName);
  }

  /// <inheritdoc />
  public override IDirectoryInfo Parent {
    get {
      return this.imaginaryFileDataAccessor_.Directory.GetParent(directoryPath);
    }
  }

  /// <inheritdoc />
  public override IDirectoryInfo Root {
    get {
      return new ImaginaryDirectoryInfo(this.imaginaryFileDataAccessor_,
                                        this.imaginaryFileDataAccessor_
                                            .Directory
                                            .GetDirectoryRoot(FullName));
    }
  }

  private ImaginaryFileData GetMockFileDataForRead() {
    if (refreshOnNextRead) {
      Refresh();
      refreshOnNextRead = false;
    }

    return this.cachedImaginaryFileData_;
  }

  private ImaginaryFileData GetMockFileDataForWrite() {
    refreshOnNextRead = true;
    return this.imaginaryFileDataAccessor_.GetFile(directoryPath) ??
           throw CommonExceptions.CouldNotFindPartOfPath(directoryPath);
  }

  /// <inheritdoc />
  public override string ToString() {
    return originalPath;
  }

  /// <inheritdoc cref="IFileSystemAclSupport.GetAccessControl()" />
  [SupportedOSPlatform("windows")]
  public object GetAccessControl() {
    return GetMockDirectoryData().AccessControl;
  }

  /// <inheritdoc cref="IFileSystemAclSupport.GetAccessControl(IFileSystemAclSupport.AccessControlSections)" />
  [SupportedOSPlatform("windows")]
  public object GetAccessControl(
      IFileSystemAclSupport.AccessControlSections includeSections) {
    return GetMockDirectoryData().AccessControl;
  }

  /// <inheritdoc cref="IFileSystemAclSupport.SetAccessControl(object)" />
  [SupportedOSPlatform("windows")]
  public void SetAccessControl(object value) {
    GetMockDirectoryData().AccessControl = value as DirectorySecurity;
  }

  private void SetDirectoryPath(string path) {
    originalPath = path;
    path = this.imaginaryFileDataAccessor_.Path.GetFullPath(path);

    path = path.TrimSlashes();
    if (ImaginaryUnixSupport.IsWindowsPlatform()) {
      path = path.TrimEnd(' ');
    }

    this.directoryPath = path;
  }

  private ImaginaryDirectoryData GetMockDirectoryData() {
    return this.imaginaryFileDataAccessor_.GetFile(directoryPath) as
               ImaginaryDirectoryData ??
           throw CommonExceptions.CouldNotFindPartOfPath(directoryPath);
  }
}