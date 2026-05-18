namespace System.IO.Abstractions.TestingHelpers;

/// <inheritdoc />
#if FEATURE_SERIALIZABLE
[Serializable]
#endif
public class ImaginaryDirectoryInfoFactory : IDirectoryInfoFactory {
  readonly IImaginaryFileDataAccessor imaginaryFileSystem_;

  /// <inheritdoc />
  public ImaginaryDirectoryInfoFactory(
      IImaginaryFileDataAccessor imaginaryFileSystem) {
    this.imaginaryFileSystem_ = imaginaryFileSystem;
  }

  /// <inheritdoc />
  public IFileSystem FileSystem
    => this.imaginaryFileSystem_;

  /// <inheritdoc />
  public IDirectoryInfo New(string path) {
    return new ImaginaryDirectoryInfo(this.imaginaryFileSystem_, path);
  }

  /// <inheritdoc />
  public IDirectoryInfo Wrap(DirectoryInfo directoryInfo) {
    if (directoryInfo == null) {
      return null;
    }

    return new ImaginaryDirectoryInfo(this.imaginaryFileSystem_,
                                 directoryInfo.FullName);
  }
}