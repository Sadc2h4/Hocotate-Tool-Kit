namespace System.IO.Abstractions.TestingHelpers;

/// <inheritdoc />
#if FEATURE_SERIALIZABLE
[Serializable]
#endif
public class ImaginaryDriveInfoFactory : IDriveInfoFactory {
  private readonly IImaginaryFileDataAccessor imaginaryFileSystem_;

  /// <inheritdoc />
  public ImaginaryDriveInfoFactory(IImaginaryFileDataAccessor imaginaryFileSystem) {
    this.imaginaryFileSystem_ = imaginaryFileSystem ??
                                throw new ArgumentNullException(
                                    nameof(imaginaryFileSystem));
  }

  /// <inheritdoc />
  public IFileSystem FileSystem
    => this.imaginaryFileSystem_;

  /// <inheritdoc />
  public IDriveInfo[] GetDrives() {
    var result = new List<DriveInfoBase>();
    foreach (string driveLetter in this.imaginaryFileSystem_.AllDrives) {
      try {
        var mockDriveInfo
            = new ImaginaryDriveInfo(this.imaginaryFileSystem_, driveLetter);
        result.Add(mockDriveInfo);
      } catch (ArgumentException) {
        // invalid drives should be ignored
      }
    }

    return result.ToArray();
  }

  /// <inheritdoc />
  public IDriveInfo New(string driveName) {
    var drive = this.imaginaryFileSystem_.Path.GetPathRoot(driveName);

    return new ImaginaryDriveInfo(this.imaginaryFileSystem_, drive);
  }

  /// <inheritdoc />
  public IDriveInfo Wrap(DriveInfo driveInfo) {
    if (driveInfo == null) {
      return null;
    }

    return New(driveInfo.Name);
  }

  private string NormalizeDriveName(string driveName) {
    if (driveName.Length == 3 &&
        this.imaginaryFileSystem_.StringOperations.EndsWith(driveName, @":\")) {
      return this.imaginaryFileSystem_.StringOperations.ToUpper(driveName[0]) +
             @":\";
    }

    if (this.imaginaryFileSystem_.StringOperations
            .StartsWith(driveName, @"\\")) {
      return null;
    }

    return driveName;
  }

  private sealed class DriveEqualityComparer : IEqualityComparer<string> {
    private readonly IImaginaryFileDataAccessor imaginaryFileSystem_;

    public DriveEqualityComparer(
        IImaginaryFileDataAccessor imaginaryFileSystem) {
      this.imaginaryFileSystem_ = imaginaryFileSystem ??
                                  throw new ArgumentNullException(
                                      nameof(imaginaryFileSystem));
    }

    public bool Equals(string x, string y) {
      return ReferenceEquals(x, y) ||
             (HasDrivePrefix(x) &&
              HasDrivePrefix(y) &&
              this.imaginaryFileSystem_.StringOperations.Equals(x[0], y[0]));
    }

    private static bool HasDrivePrefix(string x) {
      return x != null && x.Length >= 2 && x[1] == ':';
    }

    public int GetHashCode(string obj) {
      return this.imaginaryFileSystem_.StringOperations.ToUpper(obj)
                 .GetHashCode();
    }
  }
}