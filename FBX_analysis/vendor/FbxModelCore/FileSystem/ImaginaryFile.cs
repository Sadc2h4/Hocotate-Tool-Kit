using System.Text;

using Microsoft.Win32.SafeHandles;

namespace System.IO.Abstractions.TestingHelpers;

using XFS = MockUnixSupport;

/// <inheritdoc />
#if FEATURE_SERIALIZABLE
[Serializable]
#endif
public partial class ImaginaryFile : FileBase {
  private readonly IImaginaryFileDataAccessor imaginaryFileDataAccessor_;

  /// <inheritdoc />
  public ImaginaryFile(IImaginaryFileDataAccessor imaginaryFileDataAccessor) :
      base(
          imaginaryFileDataAccessor?.FileSystem) {
    this.imaginaryFileDataAccessor_ = imaginaryFileDataAccessor ??
                                      throw new ArgumentNullException(
                                          nameof(imaginaryFileDataAccessor));
  }

#if FEATURE_FILE_SPAN
  /// <inheritdoc cref="IFile.AppendAllBytes(string,byte[])"/>
  public override void AppendAllBytes(string path, byte[] bytes) {
    imaginaryFileDataAccessor_.ImaginaryPathVerifier.IsLegalAbsoluteOrRelative(
        path,
        "path");

    if (!imaginaryFileDataAccessor_.FileExists(path)) {
      this.VerifyDirectoryExists_(path);
      imaginaryFileDataAccessor_.AddFile(path,
                                         imaginaryFileDataAccessor_.AdjustTimes(
                                             new ImaginaryFileData(bytes),
                                             TimeAdjustments.All));
    } else {
      var file = imaginaryFileDataAccessor_.GetFile(path);
      file.CheckFileAccess(path, FileAccess.Write);
      imaginaryFileDataAccessor_.AdjustTimes(file,
                                             TimeAdjustments.LastAccessTime |
                                             TimeAdjustments.LastWriteTime);
      file.Contents = file.Contents.Concat(bytes).ToArray();
    }
  }

  /// <inheritdoc cref="IFile.AppendAllBytes(string,ReadOnlySpan{byte})"/>
  public override void AppendAllBytes(string path, ReadOnlySpan<byte> bytes) {
    AppendAllBytes(path, bytes.ToArray());
  }
#endif

  /// <inheritdoc />
  public override void
      AppendAllLines(string path, IEnumerable<string> contents) {
    AppendAllLines(path, contents, ImaginaryFileData.DefaultEncoding);
  }

  /// <inheritdoc />
  public override void AppendAllLines(string path,
                                      IEnumerable<string> contents,
                                      Encoding encoding) {
    this.imaginaryFileDataAccessor_.ImaginaryPathVerifier
        .IsLegalAbsoluteOrRelative(
            path,
            "path");
    VerifyValueIsNotNull_(contents, nameof(contents));
    VerifyValueIsNotNull_(encoding, nameof(encoding));

    var concatContents
        = contents.Aggregate("", (a, b) => a + b + Environment.NewLine);
    AppendAllText(path, concatContents, encoding);
  }

  /// <inheritdoc />
  public override void AppendAllText(string path, string contents) {
    AppendAllText(path, contents, ImaginaryFileData.DefaultEncoding);
  }

  /// <inheritdoc />
  public override void AppendAllText(string path,
                                     string contents,
                                     Encoding encoding) {
    this.imaginaryFileDataAccessor_.ImaginaryPathVerifier
        .IsLegalAbsoluteOrRelative(
            path,
            "path");

    if (encoding == null) {
      throw new ArgumentNullException(nameof(encoding));
    }

    if (!this.imaginaryFileDataAccessor_.FileExists(path)) {
      this.VerifyDirectoryExists_(path);
      this.imaginaryFileDataAccessor_.AddFile(path,
                                              this.imaginaryFileDataAccessor_
                                                  .AdjustTimes(
                                                      new ImaginaryFileData(
                                                          contents,
                                                          encoding),
                                                      TimeAdjustments.All));
    } else {
      var file = this.imaginaryFileDataAccessor_.GetFile(path);
      file.CheckFileAccess(path, FileAccess.Write);
      this.imaginaryFileDataAccessor_.AdjustTimes(
          file,
          TimeAdjustments.LastAccessTime | TimeAdjustments.LastWriteTime);
      var bytesToAppend = encoding.GetBytes(contents);
      file.Contents = file.Contents.Concat(bytesToAppend).ToArray();
    }
  }

#if FEATURE_FILE_SPAN
  /// <inheritdoc cref="IFile.AppendAllText(string,ReadOnlySpan{char})"/>
  public override void AppendAllText(string path, ReadOnlySpan<char> contents) {
    AppendAllText(path, contents.ToString());
  }

  /// <inheritdoc cref="IFile.AppendAllText(string,ReadOnlySpan{char},Encoding)"/>
  public override void AppendAllText(string path,
                                     ReadOnlySpan<char> contents,
                                     Encoding encoding) {
    AppendAllText(path, contents.ToString(), encoding);
  }
#endif

  /// <inheritdoc />
  public override StreamWriter AppendText(string path) {
    this.imaginaryFileDataAccessor_.ImaginaryPathVerifier
        .IsLegalAbsoluteOrRelative(
            path,
            "path");

    if (this.imaginaryFileDataAccessor_.FileExists(path)) {
      StreamWriter sw = new StreamWriter(OpenWrite(path));
      sw.BaseStream.Seek(0,
                         SeekOrigin
                             .End); //push the stream pointer at the end for append.
      return sw;
    }

    return new StreamWriter(Create(path));
  }

  /// <inheritdoc />
  public override void Copy(string sourceFileName, string destFileName) {
    Copy(sourceFileName, destFileName, false);
  }

  /// <inheritdoc />
  public override void Copy(string sourceFileName,
                            string destFileName,
                            bool overwrite) {
    if (sourceFileName == null) {
      throw CommonExceptions.FilenameCannotBeNull(nameof(sourceFileName));
    }

    if (destFileName == null) {
      throw CommonExceptions.FilenameCannotBeNull(nameof(destFileName));
    }

    this.imaginaryFileDataAccessor_.ImaginaryPathVerifier
        .IsLegalAbsoluteOrRelative(
            sourceFileName,
            nameof(sourceFileName));
    this.imaginaryFileDataAccessor_.ImaginaryPathVerifier
        .IsLegalAbsoluteOrRelative(
            destFileName,
            nameof(destFileName));

    if (!Exists(sourceFileName)) {
      throw CommonExceptions.FileNotFound(sourceFileName);
    }

    this.VerifyDirectoryExists_(destFileName);

    var fileExists = this.imaginaryFileDataAccessor_.FileExists(destFileName);
    if (fileExists) {
      if (!overwrite) {
        throw CommonExceptions.FileAlreadyExists(destFileName);
      }

      if (string.Equals(sourceFileName,
                        destFileName,
                        StringComparison.OrdinalIgnoreCase) &&
          ImaginaryUnixSupport.IsWindowsPlatform()) {
        throw CommonExceptions.ProcessCannotAccessFileInUse(destFileName);
      }

      this.imaginaryFileDataAccessor_.RemoveFile(destFileName);
    }

    var sourceFileData
        = this.imaginaryFileDataAccessor_.GetFile(sourceFileName);
    sourceFileData.CheckFileAccess(sourceFileName, FileAccess.Read);
    var destFileData = new ImaginaryFileData(sourceFileData);
    this.imaginaryFileDataAccessor_.AdjustTimes(
        destFileData,
        TimeAdjustments.CreationTime | TimeAdjustments.LastAccessTime);
    this.imaginaryFileDataAccessor_.AddFile(destFileName, destFileData);
  }

  /// <inheritdoc />
  public override FileSystemStream Create(string path) =>
      Create(path, 4096);

  /// <inheritdoc />
  public override FileSystemStream Create(string path, int bufferSize) =>
      Create(path, bufferSize, FileOptions.None);

  /// <inheritdoc />
  public override FileSystemStream Create(string path,
                                          int bufferSize,
                                          FileOptions options) =>
      CreateInternal(path, FileAccess.ReadWrite, options);

  private FileSystemStream CreateInternal(string path,
                                          FileAccess access,
                                          FileOptions options) {
    if (path == null) {
      throw new ArgumentNullException(nameof(path), "Path cannot be null.");
    }

    this.imaginaryFileDataAccessor_.ImaginaryPathVerifier
        .IsLegalAbsoluteOrRelative(
            path,
            nameof(path));
    this.VerifyDirectoryExists_(path);

    var mockFileData = new ImaginaryFileData(new byte[0]);
    this.imaginaryFileDataAccessor_.AdjustTimes(
        mockFileData,
        TimeAdjustments.All);
    this.imaginaryFileDataAccessor_.AddFile(path, mockFileData);
    return OpenInternal(path, FileMode.Open, access, options);
  }

#if FEATURE_CREATE_SYMBOLIC_LINK
  /// <inheritdoc />
  public override IFileSystemInfo CreateSymbolicLink(
      string path,
      string pathToTarget) {
    if (path == null) {
      throw CommonExceptions.FilenameCannotBeNull(nameof(path));
    }

    if (pathToTarget == null) {
      throw CommonExceptions.FilenameCannotBeNull(nameof(pathToTarget));
    }

    imaginaryFileDataAccessor_.ImaginaryPathVerifier.IsLegalAbsoluteOrRelative(
        path,
        nameof(path));
    imaginaryFileDataAccessor_.ImaginaryPathVerifier.IsLegalAbsoluteOrRelative(
        pathToTarget,
        nameof(pathToTarget));

    if (Exists(path)) {
      throw CommonExceptions.FileAlreadyExists(nameof(path));
    }

    this.VerifyDirectoryExists_(path);

    var fileExists = imaginaryFileDataAccessor_.FileExists(pathToTarget);
    if (!fileExists) {
      throw CommonExceptions.FileNotFound(pathToTarget);
    }

    var sourceFileData =
        imaginaryFileDataAccessor_.GetFile(pathToTarget);
    sourceFileData.CheckFileAccess(pathToTarget, FileAccess.Read);
    var destFileData = new ImaginaryFileData(new byte[0]);
    imaginaryFileDataAccessor_.AdjustTimes(destFileData,
                                           TimeAdjustments.CreationTime |
                                           TimeAdjustments.LastAccessTime);
    destFileData.LinkTarget = pathToTarget;
    imaginaryFileDataAccessor_.AddFile(path, destFileData);

    var mockFileInfo =
        new ImaginaryFileInfo(imaginaryFileDataAccessor_, path);
    mockFileInfo.Attributes |= FileAttributes.ReparsePoint;
    return mockFileInfo;
  }
#endif
  /// <inheritdoc />
  public override StreamWriter CreateText(string path) {
    return new StreamWriter(Create(path));
  }

  /// <inheritdoc />
  public override void Decrypt(string path) {
    this.imaginaryFileDataAccessor_.ImaginaryPathVerifier
        .IsLegalAbsoluteOrRelative(
            path,
            "path");

    new ImaginaryFileInfo(this.imaginaryFileDataAccessor_, path).Decrypt();
  }

  /// <inheritdoc />
  public override void Delete(string path) {
    this.imaginaryFileDataAccessor_.ImaginaryPathVerifier
        .IsLegalAbsoluteOrRelative(
            path,
            "path");

    // We mimic exact behavior of the standard File.Delete() method
    // which throws exception only if the folder does not exist,
    // but silently returns if deleting a non-existing file in an existing folder.
    this.VerifyDirectoryExists_(path);

    var file = this.imaginaryFileDataAccessor_.GetFile(path);
    if (file != null && !file.AllowedFileShare.HasFlag(FileShare.Delete)) {
      throw CommonExceptions.ProcessCannotAccessFileInUse(path);
    }

    if (file != null && file.IsDirectory) {
      throw new UnauthorizedAccessException(
          $"Access to the path '{path}' is denied.");
    }

    this.imaginaryFileDataAccessor_.RemoveFile(path);
  }

  /// <inheritdoc />
  public override void Encrypt(string path) {
    this.imaginaryFileDataAccessor_.ImaginaryPathVerifier
        .IsLegalAbsoluteOrRelative(
            path,
            "path");

    new ImaginaryFileInfo(this.imaginaryFileDataAccessor_, path).Encrypt();
  }

  /// <inheritdoc />
  public override bool Exists(string path) {
    if (path == null) {
      return false;
    }

    if (path.Trim() == string.Empty) {
      return false;
    }

    //Not handling exceptions here so that mock behaviour is as similar as possible to System.IO.File.Exists (See #810)
    try {
      this.imaginaryFileDataAccessor_.ImaginaryPathVerifier
          .IsLegalAbsoluteOrRelative(
              path,
              nameof(path));

      var file = this.imaginaryFileDataAccessor_.GetFile(path);
      return file != null && !file.IsDirectory;
    } catch (ArgumentException) { } catch (NotSupportedException) { } catch
        (IOException) { } catch (UnauthorizedAccessException) { }

    return false;
  }

  /// <summary>
  /// Gets the <see cref="FileAttributes"/> of the file on the path.
  /// </summary>
  /// <param name="path">The path to the file.</param>
  /// <returns>The <see cref="FileAttributes"/> of the file on the path.</returns>
  /// <exception cref="ArgumentException"><paramref name="path"/> is empty, contains only white spaces, or contains invalid characters.</exception>
  /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters, and file names must be less than 260 characters.</exception>
  /// <exception cref="NotSupportedException"><paramref name="path"/> is in an invalid format.</exception>
  /// <exception cref="FileNotFoundException"><paramref name="path"/> represents a file and is invalid, such as being on an unmapped drive, or the file cannot be found.</exception>
  /// <exception cref="DirectoryNotFoundException"><paramref name="path"/> represents a directory and is invalid, such as being on an unmapped drive, or the directory cannot be found.</exception>
  /// <exception cref="IOException">This file is being used by another process.</exception>
  /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
  public override FileAttributes GetAttributes(string path) {
    if (path != null && path.Length == 0) {
      throw CommonExceptions.PathIsNotOfALegalForm(nameof(path));
    }

    this.imaginaryFileDataAccessor_.ImaginaryPathVerifier
        .IsLegalAbsoluteOrRelative(
            path,
            "path");

    var possibleFileData = this.imaginaryFileDataAccessor_.GetFile(path);
    FileAttributes result;
    if (possibleFileData != null) {
      result = possibleFileData.Attributes;
    } else {
      var directoryInfo
          = this.imaginaryFileDataAccessor_.DirectoryInfo.New(path);
      if (directoryInfo.Exists) {
        result = directoryInfo.Attributes;
      } else {
        this.VerifyDirectoryExists_(path);

        throw CommonExceptions.FileNotFound(path);
      }
    }

    return result;
  }

#if FEATURE_FILE_ATTRIBUTES_VIA_HANDLE
  /// <inheritdoc />
  public override FileAttributes GetAttributes(SafeFileHandle fileHandle) {
    throw CommonExceptions.NotImplemented();
  }
#endif

  /// <inheritdoc />
  public override DateTime GetCreationTime(string path) {
    this.imaginaryFileDataAccessor_.ImaginaryPathVerifier
        .IsLegalAbsoluteOrRelative(
            path,
            "path");

    return GetTimeFromFile(path,
                           data => data.CreationTime.LocalDateTime,
                           () => ImaginaryFileData.DefaultDateTimeOffset
                                                  .LocalDateTime);
  }

#if FEATURE_FILE_ATTRIBUTES_VIA_HANDLE
  /// <inheritdoc />
  public override DateTime GetCreationTime(SafeFileHandle fileHandle) {
    throw CommonExceptions.NotImplemented();
  }
#endif

  /// <inheritdoc />
  public override DateTime GetCreationTimeUtc(string path) {
    this.imaginaryFileDataAccessor_.ImaginaryPathVerifier
        .IsLegalAbsoluteOrRelative(
            path,
            "path");

    return GetTimeFromFile(path,
                           data => data.CreationTime.UtcDateTime,
                           () => ImaginaryFileData.DefaultDateTimeOffset
                                                  .UtcDateTime);
  }

#if FEATURE_FILE_ATTRIBUTES_VIA_HANDLE
  /// <inheritdoc />
  public override DateTime GetCreationTimeUtc(SafeFileHandle fileHandle) {
    throw CommonExceptions.NotImplemented();
  }
#endif

  /// <inheritdoc />
  public override DateTime GetLastAccessTime(string path) {
    this.imaginaryFileDataAccessor_.ImaginaryPathVerifier
        .IsLegalAbsoluteOrRelative(
            path,
            "path");

    return GetTimeFromFile(path,
                           data => data.LastAccessTime.LocalDateTime,
                           () => ImaginaryFileData.DefaultDateTimeOffset
                                                  .LocalDateTime);
  }

#if FEATURE_FILE_ATTRIBUTES_VIA_HANDLE
  /// <inheritdoc />
  public override DateTime GetLastAccessTime(SafeFileHandle fileHandle) {
    throw CommonExceptions.NotImplemented();
  }
#endif

  /// <inheritdoc />
  public override DateTime GetLastAccessTimeUtc(string path) {
    this.imaginaryFileDataAccessor_.ImaginaryPathVerifier
        .IsLegalAbsoluteOrRelative(
            path,
            "path");

    return GetTimeFromFile(path,
                           data => data.LastAccessTime.UtcDateTime,
                           () => ImaginaryFileData.DefaultDateTimeOffset
                                                  .UtcDateTime);
  }

#if FEATURE_FILE_ATTRIBUTES_VIA_HANDLE
  /// <inheritdoc />
  public override DateTime GetLastAccessTimeUtc(SafeFileHandle fileHandle) {
    throw CommonExceptions.NotImplemented();
  }
#endif

  /// <inheritdoc />
  public override DateTime GetLastWriteTime(string path) {
    this.imaginaryFileDataAccessor_.ImaginaryPathVerifier
        .IsLegalAbsoluteOrRelative(
            path,
            "path");

    return GetTimeFromFile(path,
                           data => data.LastWriteTime.LocalDateTime,
                           () => ImaginaryFileData.DefaultDateTimeOffset
                                                  .LocalDateTime);
  }

#if FEATURE_FILE_ATTRIBUTES_VIA_HANDLE
  /// <inheritdoc />
  public override DateTime GetLastWriteTime(SafeFileHandle fileHandle) {
    throw CommonExceptions.NotImplemented();
  }
#endif

  /// <inheritdoc />
  public override DateTime GetLastWriteTimeUtc(string path) {
    this.imaginaryFileDataAccessor_.ImaginaryPathVerifier
        .IsLegalAbsoluteOrRelative(
            path,
            "path");

    return GetTimeFromFile(path,
                           data => data.LastWriteTime.UtcDateTime,
                           () => ImaginaryFileData.DefaultDateTimeOffset
                                                  .UtcDateTime);
  }

#if FEATURE_FILE_ATTRIBUTES_VIA_HANDLE
  /// <inheritdoc />
  public override DateTime GetLastWriteTimeUtc(SafeFileHandle fileHandle) {
    throw CommonExceptions.NotImplemented();
  }
#endif

#if FEATURE_UNIX_FILE_MODE
  /// <inheritdoc />
  public override UnixFileMode GetUnixFileMode(string path) {
    imaginaryFileDataAccessor_.ImaginaryPathVerifier.IsLegalAbsoluteOrRelative(
        path,
        "path");

    if (!imaginaryFileDataAccessor_.FileExists(path)) {
      throw CommonExceptions.FileNotFound(path);
    }

    var mockFileData = imaginaryFileDataAccessor_.GetFile(path);
    return mockFileData.UnixMode;
  }
#endif

#if FEATURE_FILE_ATTRIBUTES_VIA_HANDLE
  /// <inheritdoc />
  public override UnixFileMode GetUnixFileMode(SafeFileHandle fileHandle) {
    throw CommonExceptions.NotImplemented();
  }
#endif

  private DateTime GetTimeFromFile(string path,
                                   Func<ImaginaryFileData, DateTime>
                                       existingFileFunction,
                                   Func<DateTime> nonExistingFileFunction) {
    DateTime result;
    ImaginaryFileData file = this.imaginaryFileDataAccessor_.GetFile(path);
    if (file != null) {
      result = existingFileFunction(file);
    } else {
      result = nonExistingFileFunction();
    }

    return result;
  }

  /// <inheritdoc />
  public override void Move(string sourceFileName, string destFileName) {
    if (sourceFileName == null) {
      throw CommonExceptions.FilenameCannotBeNull(nameof(sourceFileName));
    }

    if (destFileName == null) {
      throw CommonExceptions.FilenameCannotBeNull(nameof(destFileName));
    }

    this.imaginaryFileDataAccessor_.ImaginaryPathVerifier
        .IsLegalAbsoluteOrRelative(
            sourceFileName,
            nameof(sourceFileName));
    this.imaginaryFileDataAccessor_.ImaginaryPathVerifier
        .IsLegalAbsoluteOrRelative(
            destFileName,
            nameof(destFileName));

    var sourceFile = this.imaginaryFileDataAccessor_.GetFile(sourceFileName);

    if (sourceFile == null) {
      throw CommonExceptions.FileNotFound(sourceFileName);
    }

    if (!sourceFile.AllowedFileShare.HasFlag(FileShare.Delete)) {
      throw CommonExceptions.ProcessCannotAccessFileInUse();
    }

    this.VerifyDirectoryExists_(destFileName);

    if (this.imaginaryFileDataAccessor_.GetFile(destFileName) != null) {
      if (this.imaginaryFileDataAccessor_.StringOperations.Equals(
              destFileName,
              sourceFileName)) {
        if (ImaginaryUnixSupport.IsWindowsPlatform()) {
          this.imaginaryFileDataAccessor_.RemoveFile(sourceFileName);
          this.imaginaryFileDataAccessor_.AddFile(
              destFileName,
              this.imaginaryFileDataAccessor_.AdjustTimes(
                  new ImaginaryFileData(sourceFile),
                  TimeAdjustments.LastAccessTime),
              false);
        }

        return;
      } else {
        throw new IOException(
            "A file can not be created if it already exists.");
      }
    }

    this.imaginaryFileDataAccessor_.RemoveFile(sourceFileName, false);
    this.imaginaryFileDataAccessor_.AddFile(destFileName,
                                            this.imaginaryFileDataAccessor_
                                                .AdjustTimes(
                                                    new ImaginaryFileData(
                                                        sourceFile),
                                                    TimeAdjustments
                                                        .LastAccessTime),
                                            false);
  }

#if FEATURE_FILE_MOVE_WITH_OVERWRITE
  /// <inheritdoc />
  public override void Move(string sourceFileName,
                            string destFileName,
                            bool overwrite) {
    if (sourceFileName == null) {
      throw CommonExceptions.FilenameCannotBeNull(nameof(sourceFileName));
    }

    if (destFileName == null) {
      throw CommonExceptions.FilenameCannotBeNull(nameof(destFileName));
    }

    imaginaryFileDataAccessor_.ImaginaryPathVerifier.IsLegalAbsoluteOrRelative(
        sourceFileName,
        nameof(sourceFileName));
    imaginaryFileDataAccessor_.ImaginaryPathVerifier.IsLegalAbsoluteOrRelative(
        destFileName,
        nameof(destFileName));

    if (imaginaryFileDataAccessor_.GetFile(destFileName) != null) {
      if (destFileName.Equals(sourceFileName)) {
        return;
      } else if (!overwrite) {
        throw new IOException(
            "A file can not be created if it already exists.");
      }
    }


    var sourceFile = imaginaryFileDataAccessor_.GetFile(sourceFileName);

    if (sourceFile == null) {
      throw CommonExceptions.FileNotFound(sourceFileName);
    }

    if (!sourceFile.AllowedFileShare.HasFlag(FileShare.Delete)) {
      throw CommonExceptions.ProcessCannotAccessFileInUse();
    }

    this.VerifyDirectoryExists_(destFileName);

    imaginaryFileDataAccessor_.RemoveFile(sourceFileName);
    imaginaryFileDataAccessor_.AddFile(destFileName,
                                       imaginaryFileDataAccessor_.AdjustTimes(
                                           new ImaginaryFileData(sourceFile),
                                           TimeAdjustments.LastAccessTime));
  }
#endif

  /// <inheritdoc />
  public override FileSystemStream Open(string path, FileMode mode) {
    this.imaginaryFileDataAccessor_.ImaginaryPathVerifier
        .IsLegalAbsoluteOrRelative(
            path,
            "path");

    return Open(path,
                mode,
                mode == FileMode.Append
                    ? FileAccess.Write
                    : FileAccess.ReadWrite,
                FileShare.None);
  }

  /// <inheritdoc />
  public override FileSystemStream Open(string path,
                                        FileMode mode,
                                        FileAccess access) {
    this.imaginaryFileDataAccessor_.ImaginaryPathVerifier
        .IsLegalAbsoluteOrRelative(
            path,
            "path");

    return Open(path, mode, access, FileShare.None);
  }

  /// <inheritdoc />
  public override FileSystemStream Open(string path,
                                        FileMode mode,
                                        FileAccess access,
                                        FileShare share) =>
      OpenInternal(path, mode, access, FileOptions.None);

#if FEATURE_FILESTREAM_OPTIONS
  /// <inheritdoc />
  public override FileSystemStream
      Open(string path, FileStreamOptions options) {
    return OpenInternal(path, options.Mode, options.Access, options.Options);
  }
#endif

  private FileSystemStream OpenInternal(
      string path,
      FileMode mode,
      FileAccess access,
      FileOptions options) {
    this.imaginaryFileDataAccessor_.ImaginaryPathVerifier
        .IsLegalAbsoluteOrRelative(
            path,
            "path");

    bool exists = this.imaginaryFileDataAccessor_.FileExists(path);

    if (mode == FileMode.CreateNew && exists) {
      throw CommonExceptions.FileAlreadyExists(path);
    }

    if ((mode == FileMode.Open || mode == FileMode.Truncate) && !exists) {
      throw CommonExceptions.FileNotFound(path);
    }

    if (!exists || mode == FileMode.CreateNew) {
      return CreateInternal(path, access, options);
    }

    if (mode == FileMode.Create || mode == FileMode.Truncate) {
      Delete(path);
      return CreateInternal(path, access, options);
    }

    var mockFileData = this.imaginaryFileDataAccessor_.GetFile(path);
    mockFileData.CheckFileAccess(path, access);
    var timeAdjustments = TimeAdjustments.LastAccessTime;
    if (access.HasFlag(FileAccess.Write)) {
      timeAdjustments |= TimeAdjustments.LastWriteTime;
    }

    this.imaginaryFileDataAccessor_.AdjustTimes(mockFileData, timeAdjustments);

    return new ImaginaryFileStream(this.imaginaryFileDataAccessor_,
                                   path,
                                   mode,
                                   access,
                                   options);
  }

  /// <inheritdoc />
  public override FileSystemStream OpenRead(string path) {
    this.imaginaryFileDataAccessor_.ImaginaryPathVerifier
        .IsLegalAbsoluteOrRelative(
            path,
            "path");

    return Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
  }

  /// <inheritdoc />
  public override StreamReader OpenText(string path) {
    this.imaginaryFileDataAccessor_.ImaginaryPathVerifier
        .IsLegalAbsoluteOrRelative(
            path,
            "path");

    return new StreamReader(OpenRead(path));
  }

  /// <inheritdoc />
  public override FileSystemStream OpenWrite(string path)
    => OpenWriteInternal(path, FileOptions.None);

  private FileSystemStream OpenWriteInternal(string path, FileOptions options) {
    this.imaginaryFileDataAccessor_.ImaginaryPathVerifier
        .IsLegalAbsoluteOrRelative(
            path,
            "path");
    return OpenInternal(path, FileMode.OpenOrCreate, FileAccess.Write, options);
  }

  /// <inheritdoc />
  public override byte[] ReadAllBytes(string path) {
    this.imaginaryFileDataAccessor_.ImaginaryPathVerifier
        .IsLegalAbsoluteOrRelative(
            path,
            "path");

    if (!this.imaginaryFileDataAccessor_.FileExists(path)) {
      throw CommonExceptions.FileNotFound(path);
    }

    this.imaginaryFileDataAccessor_.GetFile(path)
        .CheckFileAccess(path, FileAccess.Read);
    var fileData = this.imaginaryFileDataAccessor_.GetFile(path);
    this.imaginaryFileDataAccessor_.AdjustTimes(
        fileData,
        TimeAdjustments.LastAccessTime);
    return fileData.Contents.ToArray();
  }

  /// <inheritdoc />
  public override string[] ReadAllLines(string path) {
    this.imaginaryFileDataAccessor_.ImaginaryPathVerifier
        .IsLegalAbsoluteOrRelative(
            path,
            "path");

    if (!this.imaginaryFileDataAccessor_.FileExists(path)) {
      throw CommonExceptions.FileNotFound(path);
    }

    var fileData = this.imaginaryFileDataAccessor_.GetFile(path);
    fileData.CheckFileAccess(path, FileAccess.Read);
    this.imaginaryFileDataAccessor_.AdjustTimes(
        fileData,
        TimeAdjustments.LastAccessTime);

    return fileData
           .TextContents
           .SplitLines();
  }

  /// <inheritdoc />
  public override string[] ReadAllLines(string path, Encoding encoding) {
    this.imaginaryFileDataAccessor_.ImaginaryPathVerifier
        .IsLegalAbsoluteOrRelative(
            path,
            "path");

    if (encoding == null) {
      throw new ArgumentNullException(nameof(encoding));
    }

    if (!this.imaginaryFileDataAccessor_.FileExists(path)) {
      throw CommonExceptions.FileNotFound(path);
    }

    var fileData = this.imaginaryFileDataAccessor_.GetFile(path);
    fileData.CheckFileAccess(path, FileAccess.Read);
    this.imaginaryFileDataAccessor_.AdjustTimes(
        fileData,
        TimeAdjustments.LastAccessTime);

    using (var ms = new MemoryStream(fileData.Contents))
    using (var sr = new StreamReader(ms, encoding)) {
      return sr.ReadToEnd().SplitLines();
    }
  }

  /// <inheritdoc />
  public override string ReadAllText(string path) {
    return ReadAllText(path, ImaginaryFileData.DefaultEncoding);
  }

  /// <inheritdoc />
  public override string ReadAllText(string path, Encoding encoding) {
    this.imaginaryFileDataAccessor_.ImaginaryPathVerifier
        .IsLegalAbsoluteOrRelative(
            path,
            "path");

    if (!this.imaginaryFileDataAccessor_.FileExists(path)) {
      throw CommonExceptions.FileNotFound(path);
    }

    if (encoding == null) {
      throw new ArgumentNullException(nameof(encoding));
    }

    return this.ReadAllTextInternal_(path, encoding);
  }

  /// <inheritdoc />
  public override IEnumerable<string> ReadLines(string path) {
    this.imaginaryFileDataAccessor_.ImaginaryPathVerifier
        .IsLegalAbsoluteOrRelative(
            path,
            "path");

    return ReadAllLines(path);
  }

  /// <inheritdoc />
  public override IEnumerable<string>
      ReadLines(string path, Encoding encoding) {
    this.imaginaryFileDataAccessor_.ImaginaryPathVerifier
        .IsLegalAbsoluteOrRelative(
            path,
            "path");
    VerifyValueIsNotNull_(encoding, "encoding");

    return ReadAllLines(path, encoding);
  }

  /// <inheritdoc />
  public override void Replace(string sourceFileName,
                               string destinationFileName,
                               string destinationBackupFileName) {
    Replace(sourceFileName,
            destinationFileName,
            destinationBackupFileName,
            false);
  }

  /// <inheritdoc />
  public override void Replace(string sourceFileName,
                               string destinationFileName,
                               string destinationBackupFileName,
                               bool ignoreMetadataErrors) {
    if (sourceFileName == null) {
      throw new ArgumentNullException(nameof(sourceFileName));
    }

    if (destinationFileName == null) {
      throw new ArgumentNullException(nameof(destinationFileName));
    }

    if (!this.imaginaryFileDataAccessor_.FileExists(sourceFileName)) {
      throw CommonExceptions.FileNotFound(sourceFileName);
    }

    if (!this.imaginaryFileDataAccessor_.FileExists(destinationFileName)) {
      throw CommonExceptions.FileNotFound(destinationFileName);
    }

    if (this.imaginaryFileDataAccessor_.StringOperations.Equals(
            sourceFileName,
            destinationFileName) &&
        ImaginaryUnixSupport.IsWindowsPlatform()) {
      throw CommonExceptions.ProcessCannotAccessFileInUse();
    }

    if (destinationBackupFileName != null) {
      Copy(destinationFileName, destinationBackupFileName, overwrite: true);
    }

    Delete(destinationFileName);
    Move(sourceFileName, destinationFileName);
  }

#if FEATURE_CREATE_SYMBOLIC_LINK
  /// <inheritdoc />
  public override IFileSystemInfo ResolveLinkTarget(
      string linkPath,
      bool returnFinalTarget) {
    var initialContainer = imaginaryFileDataAccessor_.GetFile(linkPath);
    if (initialContainer.LinkTarget != null) {
      var nextLocation = initialContainer.LinkTarget;
      var nextContainer =
          imaginaryFileDataAccessor_.GetFile(nextLocation);

      if (returnFinalTarget) {
        // The maximum number of symbolic links that are followed:
        // https://learn.microsoft.com/en-us/dotnet/api/system.io.directory.resolvelinktarget?view=net-6.0#remarks
        int maxResolveLinks = XFS.IsWindowsPlatform() ? 63 : 40;
        for (int i = 1; i < maxResolveLinks; i++) {
          if (nextContainer.LinkTarget == null) {
            break;
          }

          nextLocation = nextContainer.LinkTarget;
          nextContainer =
              imaginaryFileDataAccessor_.GetFile(nextLocation);
        }

        if (nextContainer.LinkTarget != null) {
          throw CommonExceptions.NameCannotBeResolvedByTheSystem(linkPath);
        }
      }

      if (nextContainer.IsDirectory) {
        return new ImaginaryDirectoryInfo(imaginaryFileDataAccessor_,
                                          nextLocation);
      } else {
        return new ImaginaryFileInfo(imaginaryFileDataAccessor_, nextLocation);
      }
    }

    throw CommonExceptions.NameCannotBeResolvedByTheSystem(linkPath);
  }
#endif

  /// <inheritdoc />
  public override void
      SetAttributes(string path, FileAttributes fileAttributes) {
    this.imaginaryFileDataAccessor_.ImaginaryPathVerifier
        .IsLegalAbsoluteOrRelative(
            path,
            "path");

    var possibleFileData = this.imaginaryFileDataAccessor_.GetFile(path);
    if (possibleFileData == null) {
      var directoryInfo
          = this.imaginaryFileDataAccessor_.DirectoryInfo.New(path);
      if (directoryInfo.Exists) {
        directoryInfo.Attributes = fileAttributes;
      } else {
        throw CommonExceptions.FileNotFound(path);
      }
    } else {
      this.imaginaryFileDataAccessor_.AdjustTimes(
          possibleFileData,
          TimeAdjustments.LastAccessTime);
      possibleFileData.Attributes = fileAttributes;
    }
  }

#if FEATURE_FILE_ATTRIBUTES_VIA_HANDLE
  /// <inheritdoc />
  public override void SetAttributes(SafeFileHandle fileHandle,
                                     FileAttributes fileAttributes) {
    throw CommonExceptions.NotImplemented();
  }
#endif

  /// <inheritdoc />
  public override void SetCreationTime(string path, DateTime creationTime) {
    this.imaginaryFileDataAccessor_.ImaginaryPathVerifier
        .IsLegalAbsoluteOrRelative(
            path,
            "path");

    this.imaginaryFileDataAccessor_.GetFile(path).CreationTime
        = new DateTimeOffset(creationTime);
  }

#if FEATURE_FILE_ATTRIBUTES_VIA_HANDLE
  /// <inheritdoc />
  public override void SetCreationTime(SafeFileHandle fileHandle,
                                       DateTime creationTime) {
    throw CommonExceptions.NotImplemented();
  }
#endif

  /// <inheritdoc />
  public override void
      SetCreationTimeUtc(string path, DateTime creationTimeUtc) {
    this.imaginaryFileDataAccessor_.ImaginaryPathVerifier
        .IsLegalAbsoluteOrRelative(
            path,
            "path");

    this.imaginaryFileDataAccessor_.GetFile(path).CreationTime
        = new DateTimeOffset(creationTimeUtc, TimeSpan.Zero);
  }

#if FEATURE_FILE_ATTRIBUTES_VIA_HANDLE
  /// <inheritdoc />
  public override void SetCreationTimeUtc(SafeFileHandle fileHandle,
                                          DateTime creationTimeUtc) {
    throw CommonExceptions.NotImplemented();
  }
#endif

  /// <inheritdoc />
  public override void SetLastAccessTime(string path, DateTime lastAccessTime) {
    this.imaginaryFileDataAccessor_.ImaginaryPathVerifier
        .IsLegalAbsoluteOrRelative(
            path,
            "path");

    this.imaginaryFileDataAccessor_.GetFile(path).LastAccessTime
        = new DateTimeOffset(lastAccessTime);
  }

#if FEATURE_FILE_ATTRIBUTES_VIA_HANDLE
  /// <inheritdoc />
  public override void SetLastAccessTime(SafeFileHandle fileHandle,
                                         DateTime lastAccessTime) {
    throw CommonExceptions.NotImplemented();
  }
#endif

  /// <inheritdoc />
  public override void SetLastAccessTimeUtc(string path,
                                            DateTime lastAccessTimeUtc) {
    this.imaginaryFileDataAccessor_.ImaginaryPathVerifier
        .IsLegalAbsoluteOrRelative(
            path,
            "path");

    this.imaginaryFileDataAccessor_.GetFile(path).LastAccessTime
        = new DateTimeOffset(lastAccessTimeUtc, TimeSpan.Zero);
  }

#if FEATURE_FILE_ATTRIBUTES_VIA_HANDLE
  /// <inheritdoc />
  public override void SetLastAccessTimeUtc(SafeFileHandle fileHandle,
                                            DateTime lastAccessTimeUtc) {
    throw CommonExceptions.NotImplemented();
  }
#endif

  /// <inheritdoc />
  public override void SetLastWriteTime(string path, DateTime lastWriteTime) {
    this.imaginaryFileDataAccessor_.ImaginaryPathVerifier
        .IsLegalAbsoluteOrRelative(
            path,
            "path");

    this.imaginaryFileDataAccessor_.GetFile(path).LastWriteTime
        = new DateTimeOffset(lastWriteTime);
  }

#if FEATURE_FILE_ATTRIBUTES_VIA_HANDLE
  /// <inheritdoc />
  public override void SetLastWriteTime(SafeFileHandle fileHandle,
                                        DateTime lastWriteTime) {
    throw CommonExceptions.NotImplemented();
  }
#endif

  /// <inheritdoc />
  public override void SetLastWriteTimeUtc(string path,
                                           DateTime lastWriteTimeUtc) {
    this.imaginaryFileDataAccessor_.ImaginaryPathVerifier
        .IsLegalAbsoluteOrRelative(
            path,
            "path");

    this.imaginaryFileDataAccessor_.GetFile(path).LastWriteTime
        = new DateTimeOffset(lastWriteTimeUtc, TimeSpan.Zero);
  }

#if FEATURE_FILE_ATTRIBUTES_VIA_HANDLE
  /// <inheritdoc />
  public override void SetLastWriteTimeUtc(SafeFileHandle fileHandle,
                                           DateTime lastWriteTimeUtc) {
    throw CommonExceptions.NotImplemented();
  }
#endif

#if FEATURE_UNIX_FILE_MODE
  /// <inheritdoc />
  public override void SetUnixFileMode(string path, UnixFileMode mode) {
    imaginaryFileDataAccessor_.ImaginaryPathVerifier.IsLegalAbsoluteOrRelative(
        path,
        "path");

    if (!imaginaryFileDataAccessor_.FileExists(path)) {
      throw CommonExceptions.FileNotFound(path);
    }

    var mockFileData = imaginaryFileDataAccessor_.GetFile(path);
    mockFileData.UnixMode = mode;
  }
#endif

#if FEATURE_FILE_ATTRIBUTES_VIA_HANDLE
  /// <inheritdoc />
  public override void SetUnixFileMode(SafeFileHandle fileHandle,
                                       UnixFileMode mode) {
    throw CommonExceptions.NotImplemented();
  }
#endif

  /// <summary>
  /// Creates a new file, writes the specified byte array to the file, and then closes the file.
  /// If the target file already exists, it is overwritten.
  /// </summary>
  /// <param name="path">The file to write to.</param>
  /// <param name="bytes">The bytes to write to the file. </param>
  /// <exception cref="ArgumentException"><paramref name="path"/> is a zero-length string, contains only white space, or contains one or more invalid characters as defined by <see cref="Path.GetInvalidPathChars"/>.</exception>
  /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/> or contents is empty.</exception>
  /// <exception cref="PathTooLongException">
  /// The specified path, file name, or both exceed the system-defined maximum length.
  /// For example, on Windows-based platforms, paths must be less than 248 characters, and file names must be less than 260 characters.
  /// </exception>
  /// <exception cref="DirectoryNotFoundException">The specified path is invalid (for example, it is on an unmapped drive).</exception>
  /// <exception cref="IOException">An I/O error occurred while opening the file.</exception>
  /// <exception cref="UnauthorizedAccessException">
  /// path specified a file that is read-only.
  /// -or-
  /// This operation is not supported on the current platform.
  /// -or-
  /// path specified a directory.
  /// -or-
  /// The caller does not have the required permission.
  /// </exception>
  /// <exception cref="FileNotFoundException">The file specified in <paramref name="path"/> was not found.</exception>
  /// <exception cref="NotSupportedException"><paramref name="path"/> is in an invalid format.</exception>
  /// <exception cref="System.Security.SecurityException">The caller does not have the required permission.</exception>
  /// <remarks>
  /// Given a byte array and a file path, this method opens the specified file, writes the contents of the byte array to the file, and then closes the file.
  /// </remarks>
  public override void WriteAllBytes(string path, byte[] bytes) {
    VerifyValueIsNotNull_(bytes, "bytes");

    this.imaginaryFileDataAccessor_.ImaginaryPathVerifier
        .IsLegalAbsoluteOrRelative(
            path,
            "path");
    this.VerifyDirectoryExists_(path);

    this.imaginaryFileDataAccessor_.AddFile(path,
                                            this.imaginaryFileDataAccessor_
                                                .AdjustTimes(
                                                    new ImaginaryFileData(
                                                        bytes.ToArray()),
                                                    TimeAdjustments.All));
  }

#if FEATURE_FILE_SPAN
  /// <inheritdoc cref="IFile.WriteAllBytes(string,ReadOnlySpan{byte})"/>
  public override void WriteAllBytes(string path, ReadOnlySpan<byte> bytes) {
    WriteAllBytes(path, bytes.ToArray());
  }
#endif

  /// <summary>
  /// Creates a new file, writes a collection of strings to the file, and then closes the file.
  /// </summary>
  /// <param name="path">The file to write to.</param>
  /// <param name="contents">The lines to write to the file.</param>
  /// <exception cref="ArgumentException"><paramref name="path"/> is a zero-length string, contains only white space, or contains one or more invalid characters as defined by <see cref="Path.GetInvalidPathChars"/>.</exception>
  /// <exception cref="ArgumentNullException">Either <paramref name="path"/> or <paramref name="contents"/> is <see langword="null"/>.</exception>
  /// <exception cref="DirectoryNotFoundException">The specified path is invalid (for example, it is on an unmapped drive).</exception>
  /// <exception cref="FileNotFoundException">The file specified in <paramref name="path"/> was not found.</exception>
  /// <exception cref="IOException">An I/O error occurred while opening the file.</exception>
  /// <exception cref="PathTooLongException">
  /// The specified path, file name, or both exceed the system-defined maximum length.
  /// For example, on Windows-based platforms, paths must be less than 248 characters, and file names must be less than 260 characters.
  /// </exception>
  /// <exception cref="NotSupportedException"><paramref name="path"/> is in an invalid format.</exception>
  /// <exception cref="System.Security.SecurityException">The caller does not have the required permission.</exception>
  /// <exception cref="UnauthorizedAccessException">
  /// <paramref name="path"/> specified a file that is read-only.
  /// -or-
  /// This operation is not supported on the current platform.
  /// -or-
  /// <paramref name="path"/> specified a directory.
  /// -or-
  /// The caller does not have the required permission.
  /// </exception>
  /// <remarks>
  /// <para>
  ///     If the target file already exists, it is overwritten.
  /// </para>
  /// <para>
  ///     You can use this method to create the contents for a collection class that takes an <see cref="IEnumerable{T}"/> in its constructor, such as a <see cref="List{T}"/>, <see cref="HashSet{T}"/>, or a <see cref="SortedSet{T}"/> class.
  /// </para>
  /// </remarks>
  public override void
      WriteAllLines(string path, IEnumerable<string> contents) {
    this.imaginaryFileDataAccessor_.ImaginaryPathVerifier
        .IsLegalAbsoluteOrRelative(
            path,
            "path");
    VerifyValueIsNotNull_(contents, "contents");

    WriteAllLines(path, contents, ImaginaryFileData.DefaultEncoding);
  }

  /// <summary>
  /// Creates a new file by using the specified encoding, writes a collection of strings to the file, and then closes the file.
  /// </summary>
  /// <param name="path">The file to write to.</param>
  /// <param name="contents">The lines to write to the file.</param>
  /// <param name="encoding">The character encoding to use.</param>
  /// <exception cref="ArgumentException"><paramref name="path"/> is a zero-length string, contains only white space, or contains one or more invalid characters as defined by <see cref="Path.GetInvalidPathChars"/>.</exception>
  /// <exception cref="ArgumentNullException">Either <paramref name="path"/>, <paramref name="contents"/>, or <paramref name="encoding"/> is <see langword="null"/>.</exception>
  /// <exception cref="DirectoryNotFoundException">The specified path is invalid (for example, it is on an unmapped drive).</exception>
  /// <exception cref="FileNotFoundException">The file specified in <paramref name="path"/> was not found.</exception>
  /// <exception cref="IOException">An I/O error occurred while opening the file.</exception>
  /// <exception cref="PathTooLongException">
  /// The specified path, file name, or both exceed the system-defined maximum length.
  /// For example, on Windows-based platforms, paths must be less than 248 characters, and file names must be less than 260 characters.
  /// </exception>
  /// <exception cref="NotSupportedException"><paramref name="path"/> is in an invalid format.</exception>
  /// <exception cref="System.Security.SecurityException">The caller does not have the required permission.</exception>
  /// <exception cref="UnauthorizedAccessException">
  /// <paramref name="path"/> specified a file that is read-only.
  /// -or-
  /// This operation is not supported on the current platform.
  /// -or-
  /// <paramref name="path"/> specified a directory.
  /// -or-
  /// The caller does not have the required permission.
  /// </exception>
  /// <remarks>
  /// <para>
  ///     If the target file already exists, it is overwritten.
  /// </para>
  /// <para>
  ///     You can use this method to create a file that contains the following:
  /// <list type="bullet">
  /// <item>
  /// <description>The results of a LINQ to Objects query on the lines of a file, as obtained by using the ReadLines method.</description>
  /// </item>
  /// <item>
  /// <description>The contents of a collection that implements an <see cref="IEnumerable{T}"/> of strings.</description>
  /// </item>
  /// </list>
  /// </para>
  /// </remarks>
  public override void WriteAllLines(string path,
                                     IEnumerable<string> contents,
                                     Encoding encoding) {
    this.imaginaryFileDataAccessor_.ImaginaryPathVerifier
        .IsLegalAbsoluteOrRelative(
            path,
            "path");
    VerifyValueIsNotNull_(contents, "contents");
    VerifyValueIsNotNull_(encoding, "encoding");

    var sb = new StringBuilder();
    foreach (var line in contents) {
      sb.AppendLine(line);
    }

    WriteAllText(path, sb.ToString(), encoding);
  }

  /// <summary>
  /// Creates a new file, writes the specified string array to the file by using the specified encoding, and then closes the file.
  /// </summary>
  /// <param name="path">The file to write to.</param>
  /// <param name="contents">The string array to write to the file.</param>
  /// <exception cref="ArgumentException"><paramref name="path"/> is a zero-length string, contains only white space, or contains one or more invalid characters as defined by <see cref="Path.GetInvalidPathChars"/>.</exception>
  /// <exception cref="ArgumentNullException">Either <paramref name="path"/> or <paramref name="contents"/> is <see langword="null"/>.</exception>
  /// <exception cref="PathTooLongException">
  /// The specified path, file name, or both exceed the system-defined maximum length.
  /// For example, on Windows-based platforms, paths must be less than 248 characters, and file names must be less than 260 characters.
  /// </exception>
  /// <exception cref="DirectoryNotFoundException">The specified path is invalid (for example, it is on an unmapped drive).</exception>
  /// <exception cref="IOException">An I/O error occurred while opening the file.</exception>
  /// <exception cref="UnauthorizedAccessException">
  /// <paramref name="path"/> specified a file that is read-only.
  /// -or-
  /// This operation is not supported on the current platform.
  /// -or-
  /// <paramref name="path"/> specified a directory.
  /// -or-
  /// The caller does not have the required permission.
  /// </exception>
  /// <exception cref="FileNotFoundException">The file specified in <paramref name="path"/> was not found.</exception>
  /// <exception cref="NotSupportedException"><paramref name="path"/> is in an invalid format.</exception>
  /// <exception cref="System.Security.SecurityException">The caller does not have the required permission.</exception>
  /// <remarks>
  /// <para>
  ///     If the target file already exists, it is overwritten.
  /// </para>
  /// <para>
  ///     The default behavior of the WriteAllLines method is to write out data using UTF-8 encoding without a byte order mark (BOM). If it is necessary to include a UTF-8 identifier, such as a byte order mark, at the beginning of a file, use the <see cref="FileBase.WriteAllLines(string,string[],System.Text.Encoding)"/> method overload with <see cref="UTF8Encoding"/> encoding.
  /// </para>
  /// <para>
  ///     Given a string array and a file path, this method opens the specified file, writes the string array to the file using the specified encoding,
  ///     and then closes the file.
  /// </para>
  /// </remarks>
  public override void WriteAllLines(string path, string[] contents) {
    this.imaginaryFileDataAccessor_.ImaginaryPathVerifier
        .IsLegalAbsoluteOrRelative(
            path,
            "path");
    VerifyValueIsNotNull_(contents, "contents");

    WriteAllLines(path, contents, ImaginaryFileData.DefaultEncoding);
  }

  /// <summary>
  /// Creates a new file, writes the specified string array to the file by using the specified encoding, and then closes the file.
  /// </summary>
  /// <param name="path">The file to write to.</param>
  /// <param name="contents">The string array to write to the file.</param>
  /// <param name="encoding">An <see cref="Encoding"/> object that represents the character encoding applied to the string array.</param>
  /// <exception cref="ArgumentException"><paramref name="path"/> is a zero-length string, contains only white space, or contains one or more invalid characters as defined by <see cref="Path.GetInvalidPathChars"/>.</exception>
  /// <exception cref="ArgumentNullException">Either <paramref name="path"/> or <paramref name="contents"/> is <see langword="null"/>.</exception>
  /// <exception cref="PathTooLongException">
  /// The specified path, file name, or both exceed the system-defined maximum length.
  /// For example, on Windows-based platforms, paths must be less than 248 characters, and file names must be less than 260 characters.
  /// </exception>
  /// <exception cref="DirectoryNotFoundException">The specified path is invalid (for example, it is on an unmapped drive).</exception>
  /// <exception cref="IOException">An I/O error occurred while opening the file.</exception>
  /// <exception cref="UnauthorizedAccessException">
  /// <paramref name="path"/> specified a file that is read-only.
  /// -or-
  /// This operation is not supported on the current platform.
  /// -or-
  /// <paramref name="path"/> specified a directory.
  /// -or-
  /// The caller does not have the required permission.
  /// </exception>
  /// <exception cref="FileNotFoundException">The file specified in <paramref name="path"/> was not found.</exception>
  /// <exception cref="NotSupportedException"><paramref name="path"/> is in an invalid format.</exception>
  /// <exception cref="System.Security.SecurityException">The caller does not have the required permission.</exception>
  /// <remarks>
  /// <para>
  ///     If the target file already exists, it is overwritten.
  /// </para>
  /// <para>
  ///     Given a string array and a file path, this method opens the specified file, writes the string array to the file using the specified encoding,
  ///     and then closes the file.
  /// </para>
  /// </remarks>
  public override void WriteAllLines(string path,
                                     string[] contents,
                                     Encoding encoding) {
    this.imaginaryFileDataAccessor_.ImaginaryPathVerifier
        .IsLegalAbsoluteOrRelative(
            path,
            "path");
    VerifyValueIsNotNull_(contents, "contents");
    VerifyValueIsNotNull_(encoding, "encoding");

    WriteAllLines(path, new List<string>(contents), encoding);
  }

  /// <summary>
  /// Creates a new file, writes the specified string to the file using the specified encoding, and then closes the file. If the target file already exists, it is overwritten.
  /// </summary>
  /// <param name="path">The file to write to. </param>
  /// <param name="contents">The string to write to the file. </param>
  /// <exception cref="ArgumentException"><paramref name="path"/> is a zero-length string, contains only white space, or contains one or more invalid characters as defined by <see cref="Path.GetInvalidPathChars"/>.</exception>
  /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/> or contents is empty.</exception>
  /// <exception cref="PathTooLongException">
  /// The specified path, file name, or both exceed the system-defined maximum length.
  /// For example, on Windows-based platforms, paths must be less than 248 characters, and file names must be less than 260 characters.
  /// </exception>
  /// <exception cref="DirectoryNotFoundException">The specified path is invalid (for example, it is on an unmapped drive).</exception>
  /// <exception cref="IOException">An I/O error occurred while opening the file.</exception>
  /// <exception cref="UnauthorizedAccessException">
  /// path specified a file that is read-only.
  /// -or-
  /// This operation is not supported on the current platform.
  /// -or-
  /// path specified a directory.
  /// -or-
  /// The caller does not have the required permission.
  /// </exception>
  /// <exception cref="FileNotFoundException">The file specified in <paramref name="path"/> was not found.</exception>
  /// <exception cref="NotSupportedException"><paramref name="path"/> is in an invalid format.</exception>
  /// <exception cref="System.Security.SecurityException">The caller does not have the required permission.</exception>
  /// <remarks>
  /// This method uses UTF-8 encoding without a Byte-Order Mark (BOM), so using the <see cref="M:Encoding.GetPreamble"/> method will return an empty byte array.
  /// If it is necessary to include a UTF-8 identifier, such as a byte order mark, at the beginning of a file, use the <see cref="FileBase.WriteAllText(string,string,System.Text.Encoding)"/> method overload with <see cref="UTF8Encoding"/> encoding.
  /// <para>
  /// Given a string and a file path, this method opens the specified file, writes the string to the file, and then closes the file.
  /// </para>
  /// </remarks>
  public override void WriteAllText(string path, string contents) {
    WriteAllText(path, contents, ImaginaryFileData.DefaultEncoding);
  }

  /// <summary>
  /// Creates a new file, writes the specified string to the file using the specified encoding, and then closes the file. If the target file already exists, it is overwritten.
  /// </summary>
  /// <param name="path">The file to write to. </param>
  /// <param name="contents">The string to write to the file. </param>
  /// <param name="encoding">The encoding to apply to the string.</param>
  /// <exception cref="ArgumentException"><paramref name="path"/> is a zero-length string, contains only white space, or contains one or more invalid characters as defined by <see cref="Path.GetInvalidPathChars"/>.</exception>
  /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/> or contents is empty.</exception>
  /// <exception cref="PathTooLongException">
  /// The specified path, file name, or both exceed the system-defined maximum length.
  /// For example, on Windows-based platforms, paths must be less than 248 characters, and file names must be less than 260 characters.
  /// </exception>
  /// <exception cref="DirectoryNotFoundException">The specified path is invalid (for example, it is on an unmapped drive).</exception>
  /// <exception cref="IOException">An I/O error occurred while opening the file.</exception>
  /// <exception cref="UnauthorizedAccessException">
  /// path specified a file that is read-only.
  /// -or-
  /// This operation is not supported on the current platform.
  /// -or-
  /// path specified a directory.
  /// -or-
  /// The caller does not have the required permission.
  /// </exception>
  /// <exception cref="FileNotFoundException">The file specified in <paramref name="path"/> was not found.</exception>
  /// <exception cref="NotSupportedException"><paramref name="path"/> is in an invalid format.</exception>
  /// <exception cref="System.Security.SecurityException">The caller does not have the required permission.</exception>
  /// <remarks>
  /// Given a string and a file path, this method opens the specified file, writes the string to the file using the specified encoding, and then closes the file.
  /// The file handle is guaranteed to be closed by this method, even if exceptions are raised.
  /// </remarks>
  public override void WriteAllText(string path,
                                    string contents,
                                    Encoding encoding) {
    this.imaginaryFileDataAccessor_.ImaginaryPathVerifier
        .IsLegalAbsoluteOrRelative(
            path,
            "path");
    VerifyValueIsNotNull_(path, "path");

    if (this.imaginaryFileDataAccessor_.Directory.Exists(path)) {
      throw CommonExceptions.AccessDenied(path);
    }

    this.VerifyDirectoryExists_(path);

    ImaginaryFileData data = contents == null
        ? new ImaginaryFileData(new byte[0])
        : new ImaginaryFileData(contents, encoding);
    this.imaginaryFileDataAccessor_.AddFile(path,
                                            this.imaginaryFileDataAccessor_
                                                .AdjustTimes(
                                                    data,
                                                    TimeAdjustments.All));
  }

#if FEATURE_FILE_SPAN
  /// <inheritdoc cref="IFile.WriteAllText(string,ReadOnlySpan{char})"/>
  public override void WriteAllText(string path, ReadOnlySpan<char> contents) {
    WriteAllText(path, contents.ToString());
  }

  /// <inheritdoc cref="IFile.WriteAllText(string,ReadOnlySpan{char},Encoding)"/>
  public override void WriteAllText(string path,
                                    ReadOnlySpan<char> contents,
                                    Encoding encoding) {
    WriteAllText(path, contents.ToString(), encoding);
  }
#endif

  internal static string ReadAllBytes(byte[] contents, Encoding encoding) {
    using (var ms = new MemoryStream(contents))
    using (var sr = new StreamReader(ms, encoding)) {
      return sr.ReadToEnd();
    }
  }

  private string ReadAllTextInternal_(string path, Encoding encoding) {
    var mockFileData = this.imaginaryFileDataAccessor_.GetFile(path);
    mockFileData.CheckFileAccess(path, FileAccess.Read);
    this.imaginaryFileDataAccessor_.AdjustTimes(
        mockFileData,
        TimeAdjustments.LastAccessTime);
    return ReadAllBytes(mockFileData.Contents, encoding);
  }

  private static void VerifyValueIsNotNull_(object value, string parameterName) {
    if (value == null) {
      throw new ArgumentNullException(parameterName,
                                      StringResources.Manager.GetString(
                                          "VALUE_CANNOT_BE_NULL"));
    }
  }

  private void VerifyDirectoryExists_(string path) {
    var pathOps = this.imaginaryFileDataAccessor_.Path;
    var dir = pathOps.GetDirectoryName(pathOps.GetFullPath(path));

    if (!this.imaginaryFileDataAccessor_.Directory.Exists(dir)) {
      throw CommonExceptions.CouldNotFindPartOfPath(path);
    }
  }
}