using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;

namespace fin.io.filesystem;

public partial class ComplexFileSystem {
  public IDirectory Directory { get; }

  private sealed class DirectoryImpl(ComplexFileSystem impl) : IDirectory {
    public IFileSystem FileSystem => impl;

    // Tricky
    public void Move(string sourceDirName, string destDirName) {
      var sourceFileSystem = impl.GetFileSystemForPath_(sourceDirName);
      var destFileSystem = impl.GetFileSystemForPath_(destDirName);

      if (sourceFileSystem == destFileSystem) {
        sourceFileSystem.Directory.Move(sourceDirName, destDirName);
        return;
      }

      throw new NotImplementedException();
    }

    public string[] GetLogicalDrives() {
      throw new NotImplementedException();
    }

    public void SetCurrentDirectory(string path) {
      var currentFilesystem = impl.GetFileSystemForPath_(path);
      impl.currentFileSystem_ = currentFilesystem;

      currentFilesystem.Directory.SetCurrentDirectory(path);
    }

    public string GetCurrentDirectory()
      => impl.currentFileSystem_.Directory.GetCurrentDirectory();

    public IDirectoryInfo CreateTempSubdirectory(string? prefix = null) {
      var dir = impl.imaginary_.Directory;
      var path = System.IO.Path.Join(ImaginaryFileSystem.TEMP_DIRECTORY,
                                     Guid.CreateVersion7().ToString());

      return dir.CreateDirectory(path);
    }

    // Simple
    public IDirectoryInfo CreateDirectory(string path)
      => impl
         .GetFileSystemForPath_(path)
         .Directory
         .CreateDirectory(path);

    public IDirectoryInfo CreateDirectory(
        string path,
        UnixFileMode unixCreateMode)
      => impl
         .GetFileSystemForPath_(path)
         .Directory
         .CreateDirectory(path, unixCreateMode);

    public IFileSystemInfo CreateSymbolicLink(
        string path,
        string pathToTarget)
      => impl
         .GetFileSystemForPath_(path)
         .Directory
         .CreateSymbolicLink(path, pathToTarget);

    public void Delete(string path)
      => impl
         .GetFileSystemForPath_(path)
         .Directory
         .Delete(path);

    public void Delete(string path, bool recursive)
      => impl
         .GetFileSystemForPath_(path)
         .Directory
         .Delete(path, recursive);

    public IEnumerable<string> EnumerateDirectories(string path)
      => impl
         .GetFileSystemForPath_(path)
         .Directory
         .EnumerateDirectories(path);

    public IEnumerable<string> EnumerateDirectories(
        string path,
        string searchPattern)
      => impl
         .GetFileSystemForPath_(path)
         .Directory
         .EnumerateDirectories(path, searchPattern);

    public IEnumerable<string> EnumerateDirectories(
        string path,
        string searchPattern,
        SearchOption searchOption)
      => impl
         .GetFileSystemForPath_(path)
         .Directory
         .EnumerateDirectories(path, searchPattern, searchOption);

    public IEnumerable<string> EnumerateDirectories(
        string path,
        string searchPattern,
        EnumerationOptions enumerationOptions)
      => impl
         .GetFileSystemForPath_(path)
         .Directory
         .EnumerateDirectories(path, searchPattern, enumerationOptions);

    public IEnumerable<string> EnumerateFiles(string path)
      => impl
         .GetFileSystemForPath_(path)
         .Directory
         .EnumerateFiles(path);

    public IEnumerable<string> EnumerateFiles(
        string path,
        string searchPattern)
      => impl
         .GetFileSystemForPath_(path)
         .Directory
         .EnumerateFiles(path, searchPattern);

    public IEnumerable<string> EnumerateFiles(
        string path,
        string searchPattern,
        SearchOption searchOption)
      => impl
         .GetFileSystemForPath_(path)
         .Directory
         .EnumerateFiles(path, searchPattern, searchOption);

    public IEnumerable<string> EnumerateFiles(
        string path,
        string searchPattern,
        EnumerationOptions enumerationOptions)
      => impl
         .GetFileSystemForPath_(path)
         .Directory
         .EnumerateFiles(path, searchPattern, enumerationOptions);

    public IEnumerable<string> EnumerateFileSystemEntries(string path)
      => impl
         .GetFileSystemForPath_(path)
         .Directory
         .EnumerateFileSystemEntries(path);

    public IEnumerable<string> EnumerateFileSystemEntries(
        string path,
        string searchPattern)
      => impl
         .GetFileSystemForPath_(path)
         .Directory
         .EnumerateFileSystemEntries(path, searchPattern);

    public IEnumerable<string> EnumerateFileSystemEntries(string path,
      string searchPattern,
      SearchOption searchOption)
      => impl
         .GetFileSystemForPath_(path)
         .Directory
         .EnumerateFileSystemEntries(path, searchPattern, searchOption);

    public IEnumerable<string> EnumerateFileSystemEntries(string path,
      string searchPattern,
      EnumerationOptions enumerationOptions)
      => impl
         .GetFileSystemForPath_(path)
         .Directory
         .EnumerateFileSystemEntries(path, searchPattern, enumerationOptions);

    public bool Exists([NotNullWhen(true)] string? path)
      => impl
         .GetFileSystemForPath_(path!)
         .Directory
         .Exists(path);

    public DateTime GetCreationTime(string path)
      => impl
         .GetFileSystemForPath_(path)
         .Directory
         .GetCreationTime(path);

    public DateTime GetCreationTimeUtc(string path)
      => impl
         .GetFileSystemForPath_(path)
         .Directory
         .GetCreationTimeUtc(path);

    public string[] GetDirectories(string path)
      => impl
         .GetFileSystemForPath_(path)
         .Directory
         .GetDirectories(path);

    public string[] GetDirectories(string path, string searchPattern)
      => impl
         .GetFileSystemForPath_(path)
         .Directory
         .GetDirectories(path, searchPattern);

    public string[] GetDirectories(string path,
                                   string searchPattern,
                                   SearchOption searchOption)
      => impl
         .GetFileSystemForPath_(path)
         .Directory
         .GetDirectories(path, searchPattern, searchOption);

    public string[] GetDirectories(string path,
                                   string searchPattern,
                                   EnumerationOptions enumerationOptions)
      => impl
         .GetFileSystemForPath_(path)
         .Directory
         .GetDirectories(path, searchPattern, enumerationOptions);

    public string GetDirectoryRoot(string path)
      => impl
         .GetFileSystemForPath_(path)
         .Directory
         .GetDirectoryRoot(path);

    public string[] GetFiles(string path)
      => impl
         .GetFileSystemForPath_(path)
         .Directory
         .GetFiles(path);

    public string[] GetFiles(string path, string searchPattern)
      => impl
         .GetFileSystemForPath_(path)
         .Directory
         .GetFiles(path, searchPattern);

    public string[] GetFiles(string path,
                             string searchPattern,
                             SearchOption searchOption)
      => impl
         .GetFileSystemForPath_(path)
         .Directory
         .GetFiles(path, searchPattern, searchOption);

    public string[] GetFiles(string path,
                             string searchPattern,
                             EnumerationOptions enumerationOptions)
      => impl
         .GetFileSystemForPath_(path)
         .Directory
         .GetFiles(path, searchPattern, enumerationOptions);

    public string[] GetFileSystemEntries(string path)
      => impl
         .GetFileSystemForPath_(path)
         .Directory
         .GetFileSystemEntries(path);

    public string[] GetFileSystemEntries(string path, string searchPattern)
      => impl
         .GetFileSystemForPath_(path)
         .Directory
         .GetFileSystemEntries(path, searchPattern);

    public string[] GetFileSystemEntries(string path,
                                         string searchPattern,
                                         SearchOption searchOption)
      => impl
         .GetFileSystemForPath_(path)
         .Directory
         .GetFileSystemEntries(path, searchPattern, searchOption);

    public string[] GetFileSystemEntries(
        string path,
        string searchPattern,
        EnumerationOptions enumerationOptions)
      => impl
         .GetFileSystemForPath_(path)
         .Directory
         .GetFileSystemEntries(path, searchPattern, enumerationOptions);

    public DateTime GetLastAccessTime(string path)
      => impl
         .GetFileSystemForPath_(path)
         .Directory
         .GetLastAccessTime(path);

    public DateTime GetLastAccessTimeUtc(string path)
      => impl
         .GetFileSystemForPath_(path)
         .Directory
         .GetLastAccessTimeUtc(path);

    public DateTime GetLastWriteTime(string path)
      => impl
         .GetFileSystemForPath_(path)
         .Directory
         .GetLastWriteTime(path);

    public DateTime GetLastWriteTimeUtc(string path)
      => impl
         .GetFileSystemForPath_(path)
         .Directory
         .GetLastWriteTimeUtc(path);

    public IDirectoryInfo? GetParent(string path)
      => impl
         .GetFileSystemForPath_(path)
         .Directory
         .GetParent(path);

    public IFileSystemInfo? ResolveLinkTarget(string linkPath,
                                              bool returnFinalTarget)
      => impl
         .GetFileSystemForPath_(linkPath)
         .Directory
         .ResolveLinkTarget(linkPath, returnFinalTarget);

    public void SetCreationTime(string path, DateTime creationTime)
      => impl
         .GetFileSystemForPath_(path)
         .Directory
         .SetCreationTime(path, creationTime);

    public void SetCreationTimeUtc(string path, DateTime creationTimeUtc)
      => impl
         .GetFileSystemForPath_(path)
         .Directory
         .SetCreationTimeUtc(path, creationTimeUtc);

    public void SetLastAccessTime(string path, DateTime lastAccessTime)
      => impl
         .GetFileSystemForPath_(path)
         .Directory
         .SetLastAccessTime(path, lastAccessTime);

    public void SetLastAccessTimeUtc(string path, DateTime lastAccessTimeUtc)
      => impl
         .GetFileSystemForPath_(path)
         .Directory
         .SetLastAccessTimeUtc(path, lastAccessTimeUtc);

    public void SetLastWriteTime(string path, DateTime lastWriteTime)
      => impl
         .GetFileSystemForPath_(path)
         .Directory
         .SetLastWriteTime(path, lastWriteTime);

    public void SetLastWriteTimeUtc(string path, DateTime lastWriteTimeUtc)
      => impl
         .GetFileSystemForPath_(path)
         .Directory
         .SetLastWriteTime(path, lastWriteTimeUtc);
  }
}