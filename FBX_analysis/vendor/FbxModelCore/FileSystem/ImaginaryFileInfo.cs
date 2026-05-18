using System.Runtime.Versioning;
using System.Security.AccessControl;

namespace System.IO.Abstractions.TestingHelpers;

/// <inheritdoc />
#if FEATURE_SERIALIZABLE
[Serializable]
#endif
public class ImaginaryFileInfo : FileInfoBase, IFileSystemAclSupport {
  private readonly IImaginaryFileDataAccessor imaginaryFileSystem_;
  private string path;
  private readonly string originalPath;
  private ImaginaryFileData cachedImaginaryFileData_;
  private ImaginaryFile imaginaryFile_;
  private bool refreshOnNextRead;

  /// <inheritdoc />
  public ImaginaryFileInfo(IImaginaryFileDataAccessor imaginaryFileSystem,
                      string path) : base(imaginaryFileSystem?.FileSystem) {
    this.imaginaryFileSystem_ = imaginaryFileSystem ??
                                throw new ArgumentNullException(
                                    nameof(imaginaryFileSystem));
    imaginaryFileSystem.ImaginaryPathVerifier.IsLegalAbsoluteOrRelative(path, "path");
    this.originalPath = path;
    this.path = imaginaryFileSystem.Path.GetFullPath(path);
    this.imaginaryFile_ = new ImaginaryFile(imaginaryFileSystem);
    Refresh();
  }

#if FEATURE_CREATE_SYMBOLIC_LINK
    /// <inheritdoc />
    public override void CreateAsSymbolicLink(string pathToTarget)
    {
        FileSystem.File.CreateSymbolicLink(FullName, pathToTarget);
    }
#endif

  /// <inheritdoc />
  public override void Delete() {
    refreshOnNextRead = true;
    this.imaginaryFile_.Delete(path);
  }

  /// <inheritdoc />
  public override void Refresh() {
    var mockFileData = this.imaginaryFileSystem_.GetFile(path)?.Clone();
    this.cachedImaginaryFileData_ = mockFileData ?? ImaginaryFileData.NullObject.Clone();
  }

#if FEATURE_CREATE_SYMBOLIC_LINK
    /// <inheritdoc />
    public override IFileSystemInfo ResolveLinkTarget(bool returnFinalTarget)
    {
        return FileSystem.File.ResolveLinkTarget(FullName, returnFinalTarget);
    }
#endif

  /// <inheritdoc />
  public override FileAttributes Attributes {
    get {
      var mockFileData = GetMockFileDataForRead();
      return mockFileData.Attributes;
    }
    set {
      var mockFileData = GetMockFileDataForWrite();
      mockFileData.Attributes = value & ~FileAttributes.Directory;
    }
  }

  /// <inheritdoc />
  public override DateTime CreationTime {
    get {
      var mockFileData = GetMockFileDataForRead();
      return mockFileData.CreationTime.LocalDateTime;
    }
    set {
      var mockFileData = GetMockFileDataForWrite();
      mockFileData.CreationTime
          = AdjustUnspecifiedKind(value, DateTimeKind.Local);
    }
  }

  /// <inheritdoc />
  public override DateTime CreationTimeUtc {
    get {
      var mockFileData = GetMockFileDataForRead();
      return mockFileData.CreationTime.UtcDateTime;
    }
    set {
      var mockFileData = GetMockFileDataForWrite();
      mockFileData.CreationTime
          = AdjustUnspecifiedKind(value, DateTimeKind.Utc);
    }
  }

  /// <inheritdoc />
  public override bool Exists {
    get {
      var mockFileData = GetMockFileDataForRead();
      return (int) mockFileData.Attributes != -1 && !mockFileData.IsDirectory;
    }
  }

  /// <inheritdoc />
  public override string Extension {
    get {
      // System.IO.Path.GetExtension does only string manipulation,
      // so it's safe to delegate.
      return Path.GetExtension(path);
    }
  }

  /// <inheritdoc />
  public override string FullName {
    get { return path; }
  }

  /// <inheritdoc />
  public override DateTime LastAccessTime {
    get {
      var mockFileData = GetMockFileDataForRead();
      return mockFileData.LastAccessTime.LocalDateTime;
    }
    set {
      var mockFileData = GetMockFileDataForWrite();
      mockFileData.LastAccessTime
          = AdjustUnspecifiedKind(value, DateTimeKind.Local);
    }
  }

  /// <inheritdoc />
  public override DateTime LastAccessTimeUtc {
    get {
      var mockFileData = GetMockFileDataForRead();
      return mockFileData.LastAccessTime.UtcDateTime;
    }
    set {
      var mockFileData = GetMockFileDataForWrite();
      mockFileData.LastAccessTime
          = AdjustUnspecifiedKind(value, DateTimeKind.Utc);
    }
  }

  /// <inheritdoc />
  public override DateTime LastWriteTime {
    get {
      var mockFileData = GetMockFileDataForRead();
      return mockFileData.LastWriteTime.LocalDateTime;
    }
    set {
      var mockFileData = GetMockFileDataForWrite();
      mockFileData.LastWriteTime
          = AdjustUnspecifiedKind(value, DateTimeKind.Local);
    }
  }

  /// <inheritdoc />
  public override DateTime LastWriteTimeUtc {
    get {
      var mockFileData = GetMockFileDataForRead();
      return mockFileData.LastWriteTime.UtcDateTime;
    }
    set {
      var mockFileData = GetMockFileDataForWrite();
      mockFileData.LastWriteTime
          = AdjustUnspecifiedKind(value, DateTimeKind.Utc);
    }
  }

#if FEATURE_FILE_SYSTEM_INFO_LINK_TARGET
    /// <inheritdoc />
    public override string LinkTarget
    {
        get
        {
            var imaginaryFileData = GetMockFileDataForRead();
            return imaginaryFileData.LinkTarget;
        }
    }
#endif

  /// <inheritdoc />
  public override string Name {
    get { return new ImaginaryPath(this.imaginaryFileSystem_).GetFileName(path); }
  }

  /// <inheritdoc />
  public override StreamWriter AppendText() {
    return new StreamWriter(new ImaginaryFileStream(this.imaginaryFileSystem_,
                                               FullName,
                                               FileMode.Append,
                                               FileAccess.Write));
  }

  /// <inheritdoc />
  public override IFileInfo CopyTo(string destFileName) {
    return CopyTo(destFileName, false);
  }

  /// <inheritdoc />
  public override IFileInfo CopyTo(string destFileName, bool overwrite) {
    if (destFileName == FullName) {
      return this;
    }

    this.imaginaryFile_.Copy(FullName, destFileName, overwrite);
    return this.imaginaryFileSystem_.FileInfo.New(destFileName);
  }

  /// <inheritdoc />
  public override FileSystemStream Create() {
    var result = this.imaginaryFile_.Create(FullName);
    refreshOnNextRead = true;
    return result;
  }

  /// <inheritdoc />
  public override StreamWriter CreateText() {
    var result = this.imaginaryFile_.CreateText(FullName);
    refreshOnNextRead = true;
    return result;
  }

  /// <inheritdoc />
  public override void Decrypt() {
    var mockFileData = GetMockFileDataForWrite();
    mockFileData.Attributes &= ~FileAttributes.Encrypted;
  }

  /// <inheritdoc />
  public override void Encrypt() {
    var mockFileData = GetMockFileDataForWrite();
    mockFileData.Attributes |= FileAttributes.Encrypted;
  }

  /// <inheritdoc />
  public override void MoveTo(string destFileName) {
    this.imaginaryFile_.Move(path, destFileName);
    path = this.imaginaryFileSystem_.Path.GetFullPath(destFileName);
  }

#if FEATURE_FILE_MOVE_WITH_OVERWRITE
    /// <inheritdoc />
    public override void MoveTo(string destFileName, bool overwrite)
    {
        imaginaryFile_.Move(path, destFileName, overwrite);
        path = imaginaryFileSystem_.Path.GetFullPath(destFileName);
    }
#endif

  /// <inheritdoc />
  public override FileSystemStream Open(FileMode mode) {
    return this.imaginaryFile_.Open(FullName, mode);
  }

  /// <inheritdoc />
  public override FileSystemStream Open(FileMode mode, FileAccess access) {
    return this.imaginaryFile_.Open(FullName, mode, access);
  }

  /// <inheritdoc />
  public override FileSystemStream Open(FileMode mode,
                                        FileAccess access,
                                        FileShare share) {
    return this.imaginaryFile_.Open(FullName, mode, access, share);
  }

#if FEATURE_FILESTREAM_OPTIONS
    /// <inheritdoc />
    public override FileSystemStream Open(FileStreamOptions options)
    {
        return imaginaryFile_.Open(FullName, options.Mode, options.Access, options.Share);
    }
#endif

  /// <inheritdoc />
  public override FileSystemStream OpenRead() => this.imaginaryFile_.OpenRead(path);

  /// <inheritdoc />
  public override StreamReader OpenText() => this.imaginaryFile_.OpenText(path);

  /// <inheritdoc />
  public override FileSystemStream OpenWrite() => this.imaginaryFile_.OpenWrite(path);

  /// <inheritdoc />
  public override IFileInfo Replace(string destinationFileName,
                                    string destinationBackupFileName) {
    return Replace(destinationFileName, destinationBackupFileName, false);
  }

  /// <inheritdoc />
  public override IFileInfo Replace(string destinationFileName,
                                    string destinationBackupFileName,
                                    bool ignoreMetadataErrors) {
    this.imaginaryFile_.Replace(path,
                                destinationFileName,
                                destinationBackupFileName,
                                ignoreMetadataErrors);
    return this.imaginaryFileSystem_.FileInfo.New(destinationFileName);
  }

  /// <inheritdoc />
  public override IDirectoryInfo Directory {
    get { return this.imaginaryFileSystem_.DirectoryInfo.New(DirectoryName); }
  }

  /// <inheritdoc />
  public override string DirectoryName {
    get {
      // System.IO.Path.GetDirectoryName does only string manipulation,
      // so it's safe to delegate.
      return Path.GetDirectoryName(path);
    }
  }

  /// <inheritdoc />
  public override bool IsReadOnly {
    get {
      var mockFileData = GetMockFileDataForRead();
      return (mockFileData.Attributes & FileAttributes.ReadOnly) ==
             FileAttributes.ReadOnly;
    }
    set {
      var mockFileData = GetMockFileDataForWrite();
      if (value) {
        mockFileData.Attributes |= FileAttributes.ReadOnly;
      } else {
        mockFileData.Attributes &= ~FileAttributes.ReadOnly;
      }
    }
  }

  /// <inheritdoc />
  public override long Length {
    get {
      var mockFileData = GetMockFileDataForRead();
      if (mockFileData == null || mockFileData.IsDirectory) {
        throw CommonExceptions.FileNotFound(path);
      }

      return mockFileData.Contents.Length;
    }
  }

  /// <inheritdoc />
  public override string ToString() {
    return originalPath;
  }

  /// <inheritdoc cref="IFileSystemAclSupport.GetAccessControl()" />
  [SupportedOSPlatform("windows")]
  public object GetAccessControl() {
    return GetMockFileData().AccessControl;
  }

  /// <inheritdoc cref="IFileSystemAclSupport.GetAccessControl(IFileSystemAclSupport.AccessControlSections)" />
  [SupportedOSPlatform("windows")]
  public object GetAccessControl(
      IFileSystemAclSupport.AccessControlSections includeSections) {
    return GetMockFileData().AccessControl;
  }

  /// <inheritdoc cref="IFileSystemAclSupport.SetAccessControl(object)" />
  [SupportedOSPlatform("windows")]
  public void SetAccessControl(object value) {
    GetMockFileData().AccessControl = value as FileSecurity;
  }

  private ImaginaryFileData GetMockFileData() {
    return this.imaginaryFileSystem_.GetFile(path) ??
           throw CommonExceptions.FileNotFound(path);
  }

  private static DateTime AdjustUnspecifiedKind(
      DateTime time,
      DateTimeKind fallbackKind) {
    if (time.Kind == DateTimeKind.Unspecified) {
      return DateTime.SpecifyKind(time, fallbackKind);
    }

    return time;
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
    return this.imaginaryFileSystem_.GetFile(path) ??
           throw CommonExceptions.FileNotFound(path);
  }
}