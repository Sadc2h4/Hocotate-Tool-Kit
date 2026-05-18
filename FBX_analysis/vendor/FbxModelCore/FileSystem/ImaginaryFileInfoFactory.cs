namespace System.IO.Abstractions.TestingHelpers;

/// <inheritdoc />
#if FEATURE_SERIALIZABLE
[Serializable]
#endif
public class ImaginaryFileInfoFactory : IFileInfoFactory {
  private readonly IImaginaryFileDataAccessor imaginaryFileSystem_;

  /// <inheritdoc />
  public ImaginaryFileInfoFactory(IImaginaryFileDataAccessor imaginaryFileSystem) {
    this.imaginaryFileSystem_ = imaginaryFileSystem ??
                                throw new ArgumentNullException(
                                    nameof(imaginaryFileSystem));
  }

  /// <inheritdoc />
  public IFileSystem FileSystem
    => this.imaginaryFileSystem_;

  /// <inheritdoc />
  public IFileInfo New(string fileName) {
    return new ImaginaryFileInfo(this.imaginaryFileSystem_, fileName);
  }

  /// <inheritdoc />
  public IFileInfo Wrap(FileInfo fileInfo) {
    if (fileInfo == null) {
      return null;
    }

    return new ImaginaryFileInfo(this.imaginaryFileSystem_, fileInfo.FullName);
  }
}