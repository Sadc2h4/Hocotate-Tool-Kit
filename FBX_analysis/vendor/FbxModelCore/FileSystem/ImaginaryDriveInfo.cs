namespace System.IO.Abstractions.TestingHelpers;

/// <inheritdoc />
#if FEATURE_SERIALIZABLE
[Serializable]
#endif
public class ImaginaryDriveInfo : DriveInfoBase {
  private readonly IImaginaryFileDataAccessor imaginaryFileDataAccessor_;
  private readonly string name;

  /// <inheritdoc />
  public ImaginaryDriveInfo(IImaginaryFileDataAccessor imaginaryFileDataAccessor,
                       string name) : base(
      imaginaryFileDataAccessor?.FileSystem) {
    this.imaginaryFileDataAccessor_ = imaginaryFileDataAccessor ??
                                      throw new ArgumentNullException(
                                          nameof(imaginaryFileDataAccessor));
    this.name = imaginaryFileDataAccessor.ImaginaryPathVerifier.NormalizeDriveName(name);
  }

  /// <inheritdoc />
  public override long AvailableFreeSpace {
    get {
      var mockDriveData = GetMockDriveData();
      return mockDriveData.AvailableFreeSpace;
    }
  }

  /// <inheritdoc />
  public override string DriveFormat {
    get {
      var mockDriveData = GetMockDriveData();
      return mockDriveData.DriveFormat;
    }
  }

  /// <inheritdoc />
  public override DriveType DriveType {
    get {
      var mockDriveData = GetMockDriveData();
      return mockDriveData.DriveType;
    }
  }

  /// <inheritdoc />
  public override bool IsReady {
    get {
      var mockDriveData = GetMockDriveData();
      return mockDriveData.IsReady;
    }
  }

  /// <inheritdoc />
  public override string Name {
    get { return name; }
  }

  /// <inheritdoc />
  public override IDirectoryInfo RootDirectory {
    get { return this.imaginaryFileDataAccessor_.DirectoryInfo.New(Name); }
  }

  /// <inheritdoc />
  public override long TotalFreeSpace {
    get {
      var mockDriveData = GetMockDriveData();
      return mockDriveData.TotalFreeSpace;
    }
  }

  /// <inheritdoc />
  public override long TotalSize {
    get {
      var mockDriveData = GetMockDriveData();
      return mockDriveData.TotalSize;
    }
  }

  /// <inheritdoc />
  public override string VolumeLabel {
    get {
      var mockDriveData = GetMockDriveData();
      return mockDriveData.VolumeLabel;
    }
    set {
      var mockDriveData = GetMockDriveData();
      mockDriveData.VolumeLabel = value;
    }
  }

  /// <inheritdoc />
  public override string ToString() {
    return Name;
  }

  private ImaginaryDriveData GetMockDriveData() {
    return this.imaginaryFileDataAccessor_.GetDrive(name) ??
           throw CommonExceptions.FileNotFound(name);
  }
}