using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Abstractions;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Win32.SafeHandles;

using Encoding = System.Text.Encoding;

namespace fin.io.filesystem;

public partial class ComplexFileSystem {
  public IFile File { get; }

  private sealed class FileImpl(ComplexFileSystem impl) : IFile {
    public IFileSystem FileSystem => impl;

    // Tricky
    public void Move(string sourceFileName, string destFileName) {
      var sourceFileSystem = impl.GetFileSystemForPath_(sourceFileName);
      var destFileSystem = impl.GetFileSystemForPath_(destFileName);

      if (sourceFileSystem == destFileSystem) {
        sourceFileSystem.File.Move(sourceFileName, destFileName);
        return;
      }

      throw new NotImplementedException();
    }

    public void Move(string sourceFileName,
                     string destFileName,
                     bool overwrite) {
      var sourceFileSystem = impl.GetFileSystemForPath_(sourceFileName);
      var destFileSystem = impl.GetFileSystemForPath_(destFileName);

      if (sourceFileSystem == destFileSystem) {
        sourceFileSystem.File.Move(sourceFileName, destFileName, overwrite);
        return;
      }

      throw new NotImplementedException();
    }

    public void Copy(string sourceFileName, string destFileName) {
      var sourceFileSystem = impl.GetFileSystemForPath_(sourceFileName);
      var destFileSystem = impl.GetFileSystemForPath_(destFileName);

      if (sourceFileSystem == destFileSystem) {
        sourceFileSystem.File.Copy(sourceFileName, destFileName);
        return;
      }

      throw new NotImplementedException();
    }

    public void Copy(string sourceFileName,
                     string destFileName,
                     bool overwrite) {
      var sourceFileSystem = impl.GetFileSystemForPath_(sourceFileName);
      var destFileSystem = impl.GetFileSystemForPath_(destFileName);

      if (sourceFileSystem == destFileSystem) {
        sourceFileSystem.File.Copy(sourceFileName, destFileName, overwrite);
        return;
      }

      throw new NotImplementedException();
    }

    public void Replace(string sourceFileName,
                        string destinationFileName,
                        string? destinationBackupFileName) {
      throw new NotImplementedException();
    }

    public void Replace(string sourceFileName,
                        string destinationFileName,
                        string? destinationBackupFileName,
                        bool ignoreMetadataErrors) {
      throw new NotImplementedException();
    }

    // Simple
    public void AppendAllBytes(string path, byte[] bytes)
      => impl.GetFileSystemForPath_(path)
             .File
             .AppendAllBytes(path, bytes);

    public void AppendAllBytes(string path, ReadOnlySpan<byte> bytes)
      => impl.GetFileSystemForPath_(path)
             .File
             .AppendAllBytes(path, bytes);

    public Task AppendAllBytesAsync(
        string path,
        byte[] bytes,
        CancellationToken cancellationToken = new())
      => impl.GetFileSystemForPath_(path)
             .File
             .AppendAllBytesAsync(path, bytes, cancellationToken);

    public Task AppendAllBytesAsync(
        string path,
        ReadOnlyMemory<byte> bytes,
        CancellationToken cancellationToken = new())
      => impl.GetFileSystemForPath_(path)
             .File
             .AppendAllBytesAsync(path, bytes, cancellationToken);

    public void AppendAllLines(string path, IEnumerable<string> contents)
      => impl.GetFileSystemForPath_(path)
             .File
             .AppendAllLines(path, contents);

    public void AppendAllLines(string path,
                               IEnumerable<string> contents,
                               Encoding encoding)
      => impl.GetFileSystemForPath_(path)
             .File
             .AppendAllLines(path, contents, encoding);

    public Task AppendAllLinesAsync(
        string path,
        IEnumerable<string> contents,
        CancellationToken cancellationToken = new())
      => impl.GetFileSystemForPath_(path)
             .File
             .AppendAllLinesAsync(path, contents, cancellationToken);

    public Task AppendAllLinesAsync(
        string path,
        IEnumerable<string> contents,
        Encoding encoding,
        CancellationToken cancellationToken = new())
      => impl.GetFileSystemForPath_(path)
             .File
             .AppendAllLinesAsync(path, contents, encoding, cancellationToken);

    public void AppendAllText(string path, string? contents)
      => impl.GetFileSystemForPath_(path)
             .File
             .AppendAllText(path, contents);

    public void AppendAllText(string path,
                              string? contents,
                              Encoding encoding)
      => impl.GetFileSystemForPath_(path)
             .File
             .AppendAllText(path, contents, encoding);

    public void AppendAllText(string path, ReadOnlySpan<char> contents)
      => impl.GetFileSystemForPath_(path)
             .File
             .AppendAllText(path, contents);

    public void AppendAllText(string path,
                              ReadOnlySpan<char> contents,
                              Encoding encoding)
      => impl.GetFileSystemForPath_(path)
             .File
             .AppendAllText(path, contents, encoding);

    public Task AppendAllTextAsync(
        string path,
        string? contents,
        CancellationToken cancellationToken = new())
      => impl.GetFileSystemForPath_(path)
             .File
             .AppendAllTextAsync(path, contents, cancellationToken);

    public Task AppendAllTextAsync(
        string path,
        string? contents,
        Encoding encoding,
        CancellationToken cancellationToken = new())
      => impl.GetFileSystemForPath_(path)
             .File
             .AppendAllTextAsync(path, contents, encoding, cancellationToken);

    public Task AppendAllTextAsync(
        string path,
        ReadOnlyMemory<char> contents,
        CancellationToken cancellationToken = new())
      => impl.GetFileSystemForPath_(path)
             .File
             .AppendAllTextAsync(path, contents, cancellationToken);

    public Task AppendAllTextAsync(
        string path,
        ReadOnlyMemory<char> contents,
        Encoding encoding,
        CancellationToken cancellationToken = new())
      => impl.GetFileSystemForPath_(path)
             .File
             .AppendAllTextAsync(path, contents, encoding, cancellationToken);

    public StreamWriter AppendText(string path)
      => impl.GetFileSystemForPath_(path)
             .File
             .AppendText(path);

    public FileSystemStream Create(string path)
      => impl.GetFileSystemForPath_(path)
             .File
             .Create(path);

    public FileSystemStream Create(string path, int bufferSize)
      => impl.GetFileSystemForPath_(path)
             .File
             .Create(path, bufferSize);

    public FileSystemStream Create(string path,
                                   int bufferSize,
                                   FileOptions options)
      => impl.GetFileSystemForPath_(path)
             .File
             .Create(path, bufferSize, options);

    public IFileSystemInfo CreateSymbolicLink(
        string path,
        string pathToTarget)
      => impl.GetFileSystemForPath_(path)
             .File
             .CreateSymbolicLink(path, pathToTarget);

    public StreamWriter CreateText(string path)
      => impl.GetFileSystemForPath_(path)
             .File
             .CreateText(path);

    public void Decrypt(string path) {
      if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
        throw new NotImplementedException();
      }

      impl.GetFileSystemForPath_(path)
          .File
          .Decrypt(path);
    }

    public void Encrypt(string path) {
      if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
        throw new NotImplementedException();
      }

      impl.GetFileSystemForPath_(path)
          .File
          .Encrypt(path);
    }

    public void Delete(string path)
      => impl.GetFileSystemForPath_(path)
             .File
             .Delete(path);

    public bool Exists([NotNullWhen(true)] string? path)
      => impl.GetFileSystemForPath_(path!)
             .File
             .Exists(path);

    public FileAttributes GetAttributes(string path)
      => impl.GetFileSystemForPath_(path)
             .File
             .GetAttributes(path);

    public DateTime GetCreationTime(string path)
      => impl.GetFileSystemForPath_(path)
             .File
             .GetCreationTime(path);

    public DateTime GetCreationTimeUtc(string path)
      => impl.GetFileSystemForPath_(path)
             .File
             .GetCreationTimeUtc(path);

    public DateTime GetLastAccessTime(string path)
      => impl.GetFileSystemForPath_(path)
             .File
             .GetLastAccessTime(path);

    public DateTime GetLastAccessTimeUtc(string path)
      => impl.GetFileSystemForPath_(path)
             .File
             .GetLastAccessTimeUtc(path);

    public DateTime GetLastWriteTime(string path)
      => impl.GetFileSystemForPath_(path)
             .File
             .GetLastWriteTime(path);

    public DateTime GetLastWriteTimeUtc(string path)
      => impl.GetFileSystemForPath_(path)
             .File
             .GetLastWriteTimeUtc(path);

    public UnixFileMode GetUnixFileMode(string path) {
      if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
        throw new NotImplementedException();
      }
      
      return impl.GetFileSystemForPath_(path)
                 .File
                 .GetUnixFileMode(path);
    }

    public void SetUnixFileMode(string path, UnixFileMode mode) {
      if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
        throw new NotImplementedException();
      }

      impl.GetFileSystemForPath_(path)
          .File
          .SetUnixFileMode(path, mode);
    }

    public FileSystemStream Open(string path, FileMode mode)
      => impl.GetFileSystemForPath_(path)
             .File
             .Open(path, mode);

    public FileSystemStream Open(
        string path,
        FileMode mode,
        FileAccess access)
      => impl.GetFileSystemForPath_(path)
             .File
             .Open(path, mode, access);

    public FileSystemStream Open(string path,
                                 FileMode mode,
                                 FileAccess access,
                                 FileShare share)
      => impl.GetFileSystemForPath_(path)
             .File
             .Open(path, mode, access, share);

    public FileSystemStream Open(string path, FileStreamOptions options)
      => impl.GetFileSystemForPath_(path)
             .File
             .Open(path, options);

    public FileSystemStream OpenRead(string path)
      => impl.GetFileSystemForPath_(path)
             .File
             .OpenRead(path);

    public StreamReader OpenText(string path)
      => impl.GetFileSystemForPath_(path)
             .File
             .OpenText(path);

    public FileSystemStream OpenWrite(string path)
      => impl.GetFileSystemForPath_(path)
             .File
             .OpenWrite(path);

    public byte[] ReadAllBytes(string path)
      => impl.GetFileSystemForPath_(path)
             .File
             .ReadAllBytes(path);

    public Task<byte[]> ReadAllBytesAsync(
        string path,
        CancellationToken cancellationToken = new())
      => impl.GetFileSystemForPath_(path)
             .File
             .ReadAllBytesAsync(path, cancellationToken);

    public string[] ReadAllLines(string path)
      => impl.GetFileSystemForPath_(path)
             .File
             .ReadAllLines(path);

    public string[] ReadAllLines(string path, Encoding encoding)
      => impl.GetFileSystemForPath_(path)
             .File
             .ReadAllLines(path, encoding);

    public Task<string[]> ReadAllLinesAsync(
        string path,
        CancellationToken cancellationToken = new())
      => impl.GetFileSystemForPath_(path)
             .File
             .ReadAllLinesAsync(path, cancellationToken);

    public Task<string[]> ReadAllLinesAsync(
        string path,
        Encoding encoding,
        CancellationToken cancellationToken = new())
      => impl.GetFileSystemForPath_(path)
             .File
             .ReadAllLinesAsync(path, encoding, cancellationToken);

    public string ReadAllText(string path)
      => impl.GetFileSystemForPath_(path)
             .File
             .ReadAllText(path);

    public string ReadAllText(string path, Encoding encoding)
      => impl.GetFileSystemForPath_(path)
             .File
             .ReadAllText(path, encoding);

    public Task<string> ReadAllTextAsync(
        string path,
        CancellationToken cancellationToken = new())
      => impl.GetFileSystemForPath_(path)
             .File
             .ReadAllTextAsync(path, cancellationToken);

    public Task<string> ReadAllTextAsync(
        string path,
        Encoding encoding,
        CancellationToken cancellationToken = new())
      => impl.GetFileSystemForPath_(path)
             .File
             .ReadAllTextAsync(path, encoding, cancellationToken);

    public IEnumerable<string> ReadLines(string path)
      => impl.GetFileSystemForPath_(path)
             .File
             .ReadLines(path);

    public IEnumerable<string> ReadLines(string path, Encoding encoding)
      => impl.GetFileSystemForPath_(path)
             .File
             .ReadLines(path, encoding);

    public IAsyncEnumerable<string> ReadLinesAsync(
        string path,
        CancellationToken cancellationToken = new())
      => impl.GetFileSystemForPath_(path)
             .File
             .ReadLinesAsync(path, cancellationToken);

    public IAsyncEnumerable<string> ReadLinesAsync(
        string path,
        Encoding encoding,
        CancellationToken cancellationToken = new())
      => impl.GetFileSystemForPath_(path)
             .File
             .ReadLinesAsync(path, encoding, cancellationToken);

    public IFileSystemInfo? ResolveLinkTarget(string linkPath,
                                              bool returnFinalTarget)
      => impl.GetFileSystemForPath_(linkPath)
             .File
             .ResolveLinkTarget(linkPath, returnFinalTarget);

    public void SetAttributes(string path, FileAttributes fileAttributes)
      => impl.GetFileSystemForPath_(path)
             .File
             .SetAttributes(path, fileAttributes);

    public void SetCreationTime(string path, DateTime creationTime)
      => impl.GetFileSystemForPath_(path)
             .File
             .SetCreationTime(path, creationTime);

    public void SetCreationTimeUtc(string path, DateTime creationTimeUtc)
      => impl.GetFileSystemForPath_(path)
             .File
             .SetCreationTimeUtc(path, creationTimeUtc);

    public void SetLastAccessTime(string path, DateTime lastAccessTime)
      => impl.GetFileSystemForPath_(path)
             .File
             .SetLastAccessTime(path, lastAccessTime);

    public void SetLastAccessTimeUtc(string path, DateTime lastAccessTimeUtc)
      => impl.GetFileSystemForPath_(path)
             .File
             .SetLastAccessTimeUtc(path, lastAccessTimeUtc);

    public void SetLastWriteTime(string path, DateTime lastWriteTime)
      => impl.GetFileSystemForPath_(path)
             .File
             .SetLastWriteTime(path, lastWriteTime);

    public void SetLastWriteTimeUtc(string path, DateTime lastWriteTimeUtc)
      => impl.GetFileSystemForPath_(path)
             .File
             .SetLastWriteTimeUtc(path, lastWriteTimeUtc);

    public void WriteAllBytes(string path, byte[] bytes)
      => impl.GetFileSystemForPath_(path)
             .File
             .WriteAllBytes(path, bytes);

    public void WriteAllBytes(string path, ReadOnlySpan<byte> bytes)
      => impl.GetFileSystemForPath_(path)
             .File
             .WriteAllBytes(path, bytes);

    public Task WriteAllBytesAsync(
        string path,
        byte[] bytes,
        CancellationToken cancellationToken = new())
      => impl.GetFileSystemForPath_(path)
             .File
             .WriteAllBytesAsync(path, bytes, cancellationToken);

    public Task WriteAllBytesAsync(
        string path,
        ReadOnlyMemory<byte> bytes,
        CancellationToken cancellationToken = new())
      => impl.GetFileSystemForPath_(path)
             .File
             .WriteAllBytesAsync(path, bytes, cancellationToken);

    public void WriteAllLines(string path, string[] contents)
      => impl.GetFileSystemForPath_(path)
             .File
             .WriteAllLines(path, contents);

    public void WriteAllLines(string path, IEnumerable<string> contents)
      => impl.GetFileSystemForPath_(path)
             .File
             .WriteAllLines(path, contents);

    public void WriteAllLines(string path,
                              string[] contents,
                              Encoding encoding)
      => impl.GetFileSystemForPath_(path)
             .File
             .WriteAllLines(path, contents, encoding);

    public void WriteAllLines(string path,
                              IEnumerable<string> contents,
                              Encoding encoding)
      => impl.GetFileSystemForPath_(path)
             .File
             .WriteAllLines(path, contents, encoding);

    public Task WriteAllLinesAsync(
        string path,
        IEnumerable<string> contents,
        CancellationToken cancellationToken = new())
      => impl.GetFileSystemForPath_(path)
             .File
             .WriteAllLinesAsync(path, contents, cancellationToken);

    public Task WriteAllLinesAsync(
        string path,
        IEnumerable<string> contents,
        Encoding encoding,
        CancellationToken cancellationToken = new())
      => impl.GetFileSystemForPath_(path)
             .File
             .WriteAllLinesAsync(path, contents, encoding, cancellationToken);

    public void WriteAllText(string path, string? contents)
      => impl.GetFileSystemForPath_(path)
             .File
             .WriteAllText(path, contents);

    public void WriteAllText(string path, string? contents, Encoding encoding)
      => impl.GetFileSystemForPath_(path)
             .File
             .WriteAllText(path, contents, encoding);

    public void WriteAllText(string path, ReadOnlySpan<char> contents)
      => impl.GetFileSystemForPath_(path)
             .File
             .WriteAllText(path, contents);

    public void WriteAllText(string path,
                             ReadOnlySpan<char> contents,
                             Encoding encoding)
      => impl.GetFileSystemForPath_(path)
             .File
             .WriteAllText(path, contents, encoding);

    public Task WriteAllTextAsync(
        string path,
        string? contents,
        CancellationToken cancellationToken = new())
      => impl.GetFileSystemForPath_(path)
             .File
             .WriteAllTextAsync(path, contents, cancellationToken);

    public Task WriteAllTextAsync(
        string path,
        string? contents,
        Encoding encoding,
        CancellationToken cancellationToken = new())
      => impl.GetFileSystemForPath_(path)
             .File
             .WriteAllTextAsync(path, contents, encoding, cancellationToken);

    public Task WriteAllTextAsync(
        string path,
        ReadOnlyMemory<char> contents,
        CancellationToken cancellationToken = new())
      => impl.GetFileSystemForPath_(path)
             .File
             .WriteAllTextAsync(path, contents, cancellationToken);

    public Task WriteAllTextAsync(
        string path,
        ReadOnlyMemory<char> contents,
        Encoding encoding,
        CancellationToken cancellationToken = new())
      => impl.GetFileSystemForPath_(path)
             .File
             .WriteAllTextAsync(path, contents, encoding, cancellationToken);

    // Unsupported

    public FileAttributes GetAttributes(SafeFileHandle fileHandle)
      => throw new NotImplementedException();

    public DateTime GetCreationTime(SafeFileHandle fileHandle)
      => throw new NotImplementedException();

    public DateTime GetCreationTimeUtc(SafeFileHandle fileHandle)
      => throw new NotImplementedException();

    public DateTime GetLastAccessTime(SafeFileHandle fileHandle)
      => throw new NotImplementedException();

    public DateTime GetLastAccessTimeUtc(SafeFileHandle fileHandle)
      => throw new NotImplementedException();

    public DateTime GetLastWriteTime(SafeFileHandle fileHandle)
      => throw new NotImplementedException();

    public DateTime GetLastWriteTimeUtc(SafeFileHandle fileHandle)
      => throw new NotImplementedException();

    public UnixFileMode GetUnixFileMode(SafeFileHandle fileHandle)
      => throw new NotImplementedException();

    public void SetAttributes(SafeFileHandle fileHandle,
                              FileAttributes fileAttributes)
      => throw new NotImplementedException();

    public void SetCreationTime(SafeFileHandle fileHandle,
                                DateTime creationTime)
      => throw new NotImplementedException();

    public void SetCreationTimeUtc(SafeFileHandle fileHandle,
                                   DateTime creationTimeUtc)
      => throw new NotImplementedException();

    public void SetLastAccessTime(SafeFileHandle fileHandle,
                                  DateTime lastAccessTime)
      => throw new NotImplementedException();

    public void SetLastAccessTimeUtc(SafeFileHandle fileHandle,
                                     DateTime lastAccessTimeUtc)
      => throw new NotImplementedException();

    public void SetLastWriteTime(SafeFileHandle fileHandle,
                                 DateTime lastWriteTime)
      => throw new NotImplementedException();

    public void SetLastWriteTimeUtc(SafeFileHandle fileHandle,
                                    DateTime lastWriteTimeUtc)
      => throw new NotImplementedException();

    public void SetUnixFileMode(SafeFileHandle fileHandle, UnixFileMode mode)
      => throw new NotImplementedException();
  }
}