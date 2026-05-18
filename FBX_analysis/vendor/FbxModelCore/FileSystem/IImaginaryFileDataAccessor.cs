using System.Reflection;

namespace System.IO.Abstractions.TestingHelpers;

/// <summary>
/// Provides access to the file system storage.
/// </summary>
public interface IImaginaryFileDataAccessor : IFileSystem {
  /// <summary>
  /// Adjust the times of the <paramref name="fileData"/>.
  /// </summary>
  /// <param name="fileData">The <see cref="ImaginaryFileData"/> for which the times should be adjusted.</param>
  /// <param name="timeAdjustments">The adjustments to make on the <see cref="ImaginaryFileData"/>.</param>
  /// <returns>The adjusted file.</returns>
  ImaginaryFileData AdjustTimes(ImaginaryFileData fileData,
                           TimeAdjustments timeAdjustments);

  /// <summary>
  /// Gets a file.
  /// </summary>
  /// <param name="path">The path of the file to get.</param>
  /// <returns>The file. <see langword="null"/> if the file does not exist.</returns>
  ImaginaryFileData GetFile(string path);

  /// <summary>
  /// Gets a drive.
  /// </summary>
  /// <param name="name">The name of the drive to get.</param>
  /// <returns>The drive. <see langword="null"/> if the drive does not exist.</returns>
  ImaginaryDriveData GetDrive(string name);

  /// <summary>
  /// Adds the file.
  /// </summary>
  /// <param name="path">The path of the file to add.</param>
  /// <param name="imaginaryFile">The file data to add.</param>
  /// <param name="verifyAccess">Flag indicating if the access conditions should be verified.</param>
  void AddFile(string path, ImaginaryFileData imaginaryFile, bool verifyAccess = true);

  /// <summary>
  /// </summary>
  void AddDirectory(string path);

  /// <summary>
  /// </summary>
  void AddDrive(string name, ImaginaryDriveData imaginaryDrive);

  /// <summary>
  /// </summary>
  void AddFileFromEmbeddedResource(string path,
                                   Assembly resourceAssembly,
                                   string embeddedResourcePath);

  /// <summary>
  /// </summary>
  void AddFilesFromEmbeddedNamespace(string path,
                                     Assembly resourceAssembly,
                                     string embeddedResourcePath);

  /// <summary>
  /// </summary>
  void MoveDirectory(string sourcePath, string destPath);

  /// <summary>
  /// Removes the file.
  /// </summary>
  /// <param name="path">The file to remove.</param>
  /// <param name="verifyAccess">Flag indicating if the access conditions should be verified.</param>
  /// <remarks>
  /// The file must not exist.
  /// </remarks>
  void RemoveFile(string path, bool verifyAccess = true);

  /// <summary>
  /// Determines whether the file exists.
  /// </summary>
  /// <param name="path">The file to check. </param>
  /// <returns><see langword="true"/> if the file exists; otherwise, <see langword="false"/>.</returns>
  bool FileExists(string path);

  /// <summary>
  /// Gets all unique paths of all files and directories.
  /// </summary>
  IEnumerable<string> AllPaths { get; }

  /// <summary>
  /// Gets the paths of all files.
  /// </summary>
  IEnumerable<string> AllFiles { get; }

  /// <summary>
  /// Gets the paths of all directories.
  /// </summary>
  IEnumerable<string> AllDirectories { get; }

  /// <summary>
  /// Gets the names of all drives.
  /// </summary>
  IEnumerable<string> AllDrives { get; }

  /// <summary>
  /// Gets a helper for string operations.
  /// </summary>

  StringOperations StringOperations { get; }

  /// <summary>
  /// Gets a helper for verifying file system paths.
  /// </summary>
  ImaginaryPathVerifier ImaginaryPathVerifier { get; }

  /// <summary>
  /// Gets a reference to the underlying file system. 
  /// </summary>
  IFileSystem FileSystem { get; }
}