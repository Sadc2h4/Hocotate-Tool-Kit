namespace System.IO.Abstractions.TestingHelpers;

/// <summary>
/// The class represents the associated data of a drive.
/// </summary>
#if FEATURE_SERIALIZABLE
[Serializable]
#endif
public class ImaginaryDriveData {
  /// <summary>
  /// Initializes a new instance of the <see cref="ImaginaryDriveData"/> class.
  /// </summary>
  public ImaginaryDriveData() {
    IsReady = true;
  }

  /// <summary>
  /// Initializes a new instance of the <see cref="ImaginaryDriveData"/> class by copying the given <see cref="ImaginaryDriveData"/>.
  /// </summary>
  /// <param name="template">The template instance.</param>
  /// <exception cref="ArgumentNullException">Thrown if <paramref name="template"/> is <see langword="null"/>.</exception>
  public ImaginaryDriveData(ImaginaryDriveData template) {
    if (template == null) {
      throw new ArgumentNullException(nameof(template));
    }

    AvailableFreeSpace = template.AvailableFreeSpace;
    DriveFormat = template.DriveFormat;
    DriveType = template.DriveType;
    IsReady = template.IsReady;
    TotalFreeSpace = template.TotalFreeSpace;
    TotalSize = template.TotalSize;
    VolumeLabel = template.VolumeLabel;
  }

  /// <summary>
  /// Gets or sets the amount of available free space of the <see cref="ImaginaryDriveData"/>, in bytes.
  /// </summary>
  public long AvailableFreeSpace { get; set; }

  /// <summary>
  /// Gets or sets the name of the file system of the <see cref="ImaginaryDriveData"/>, such as NTFS or FAT32.
  /// </summary>
  public string DriveFormat { get; set; }

  /// <summary>
  /// Gets or sets the drive type of the <see cref="ImaginaryDriveData"/>, such as CD-ROM, removable, network, or fixed.
  /// </summary>
  public DriveType DriveType { get; set; }

  /// <summary>
  /// Gets or sets the value that indicates whether the <see cref="ImaginaryDriveData"/> is ready.
  /// </summary>
  public bool IsReady { get; set; }

  /// <summary>
  /// Gets or sets the total amount of free space available on the <see cref="ImaginaryDriveData"/>, in bytes.
  /// </summary>
  public long TotalFreeSpace { get; set; }

  /// <summary>
  /// Gets or sets the total size of storage space on the <see cref="ImaginaryDriveData"/>, in bytes.
  /// </summary>
  public long TotalSize { get; set; }

  /// <summary>
  /// Gets or sets the volume label of the <see cref="ImaginaryDriveData"/>.
  /// </summary>
  public string VolumeLabel { get; set; }
}