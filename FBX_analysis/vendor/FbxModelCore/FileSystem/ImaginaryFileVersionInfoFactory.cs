namespace System.IO.Abstractions.TestingHelpers;

/// <inheritdoc />
#if FEATURE_SERIALIZABLE
[Serializable]
#endif
public class ImaginaryFileVersionInfoFactory : IFileVersionInfoFactory {
  private readonly IImaginaryFileDataAccessor imaginaryFileSystem_;

  /// <inheritdoc />
  public ImaginaryFileVersionInfoFactory(
      IImaginaryFileDataAccessor imaginaryFileSystem) {
    this.imaginaryFileSystem_ = imaginaryFileSystem ??
                                throw new ArgumentNullException(
                                    nameof(imaginaryFileSystem));
  }

  /// <inheritdoc />
  public IFileSystem FileSystem => this.imaginaryFileSystem_;

  /// <inheritdoc />
  public IFileVersionInfo GetVersionInfo(string fileName) {
    ImaginaryFileData imaginaryFileData = this.imaginaryFileSystem_.GetFile(fileName);

    if (imaginaryFileData != null) {
      return imaginaryFileData.FileVersionInfo;
    }

    throw CommonExceptions.FileNotFound(fileName);
  }
}