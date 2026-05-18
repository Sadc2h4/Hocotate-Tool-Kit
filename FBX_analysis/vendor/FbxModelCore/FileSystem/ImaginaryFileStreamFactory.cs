using Microsoft.Win32.SafeHandles;

namespace System.IO.Abstractions.TestingHelpers;

/// <inheritdoc />
#if FEATURE_SERIALIZABLE
[Serializable]
#endif
public class ImaginaryFileStreamFactory : IFileStreamFactory {
  private readonly IImaginaryFileDataAccessor imaginaryFileSystem_;

  /// <inheritdoc />
  public ImaginaryFileStreamFactory(IImaginaryFileDataAccessor imaginaryFileSystem)
    => this.imaginaryFileSystem_ = imaginaryFileSystem ??
                                   throw new ArgumentNullException(
                                       nameof(imaginaryFileSystem));

  /// <inheritdoc />
  public IFileSystem FileSystem
    => this.imaginaryFileSystem_;

  /// <inheritdoc />
  public FileSystemStream New(SafeFileHandle handle, FileAccess access)
    => new ImaginaryFileStream(this.imaginaryFileSystem_,
                          handle.ToString(),
                          FileMode.Open,
                          access: access);

  /// <inheritdoc />
  public FileSystemStream New(SafeFileHandle handle,
                              FileAccess access,
                              int bufferSize)
    => new ImaginaryFileStream(this.imaginaryFileSystem_,
                          handle.ToString(),
                          FileMode.Open,
                          access: access);

  /// <inheritdoc />
  public FileSystemStream New(SafeFileHandle handle,
                              FileAccess access,
                              int bufferSize,
                              bool isAsync)
    => new ImaginaryFileStream(this.imaginaryFileSystem_,
                          handle.ToString(),
                          FileMode.Open,
                          access: access);

  /// <inheritdoc />
  public FileSystemStream New(string path, FileMode mode)
    => new ImaginaryFileStream(this.imaginaryFileSystem_, path, mode);

  /// <inheritdoc />
  public FileSystemStream New(string path, FileMode mode, FileAccess access)
    => new ImaginaryFileStream(this.imaginaryFileSystem_, path, mode, access);

  /// <inheritdoc />
  public FileSystemStream New(string path,
                              FileMode mode,
                              FileAccess access,
                              FileShare share)
    => new ImaginaryFileStream(this.imaginaryFileSystem_, path, mode, access);

  /// <inheritdoc />
  public FileSystemStream New(string path,
                              FileMode mode,
                              FileAccess access,
                              FileShare share,
                              int bufferSize)
    => new ImaginaryFileStream(this.imaginaryFileSystem_, path, mode, access);

  /// <inheritdoc />
  public FileSystemStream New(string path,
                              FileMode mode,
                              FileAccess access,
                              FileShare share,
                              int bufferSize,
                              bool useAsync)
    => new ImaginaryFileStream(this.imaginaryFileSystem_, path, mode, access);

  /// <inheritdoc />
  public FileSystemStream New(string path,
                              FileMode mode,
                              FileAccess access,
                              FileShare share,
                              int bufferSize,
                              FileOptions options)
    => new ImaginaryFileStream(this.imaginaryFileSystem_,
                          path,
                          mode,
                          access,
                          options);

#if FEATURE_FILESTREAM_OPTIONS
    /// <inheritdoc />
    public FileSystemStream New(string path, FileStreamOptions options)
        => new ImaginaryFileStream(imaginaryFileSystem_, path, options.Mode, options.Access, options.Options);
#endif

  /// <inheritdoc />
  public FileSystemStream Wrap(FileStream fileStream)
    => throw new NotSupportedException(
        "You cannot wrap an existing FileStream in the ImaginaryFileSystem instance!");
}