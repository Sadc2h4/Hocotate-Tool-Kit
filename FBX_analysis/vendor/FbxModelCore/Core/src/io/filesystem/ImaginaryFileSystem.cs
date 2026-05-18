using System;
using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Reflection;

using fin.util.strings;

namespace fin.io.filesystem;

public sealed class ImaginaryFileSystem : MockFileSystem {
  public const char DRIVE = ImaginaryPathInternal.DriveChar;

  public ImaginaryFileSystem()
      : base(new MockFileSystemOptions { CreateDefaultTempDir = false }) {
    this.Path = new ImaginaryPath(this);

    typeof(MockFileSystem)
        .GetField("pathVerifier",BindingFlags.Instance|BindingFlags.NonPublic)
        .SetValue(this, new ImaginaryPathVerifier(this));

    this.AddDrive($"{DRIVE}:", new MockDriveData());
    this.Directory.CreateDirectory($"{DRIVE}:\\");
    this.Directory.SetCurrentDirectory($"{DRIVE}:\\");
  }

  public override IPath Path { get; } = new TemporaryPath();

  private class ImaginaryPath : MockPath {
    public ImaginaryPath(IMockFileDataAccessor mockFileDataAccessor) : base(
        mockFileDataAccessor) { }


  }

  private class TemporaryPath : IPath {
    public string GetFullPath(string path) => path.SubstringUpTo('\0');
    public char[] GetInvalidPathChars() => System.IO.Path.GetInvalidPathChars();

    public IFileSystem FileSystem { get; }

    [return: NotNullIfNotNull("path")]
    public string? ChangeExtension(string? path, string? extension) {
      throw new NotImplementedException();
    }

    public string Combine(string path1, string path2) {
      throw new NotImplementedException();
    }

    public string Combine(string path1, string path2, string path3) {
      throw new NotImplementedException();
    }

    public string Combine(string path1,
                          string path2,
                          string path3,
                          string path4) {
      throw new NotImplementedException();
    }

    public string Combine(params string[] paths) {
      throw new NotImplementedException();
    }

    public string Combine(params ReadOnlySpan<string> paths) {
      throw new NotImplementedException();
    }

    public bool EndsInDirectorySeparator(ReadOnlySpan<char> path) {
      throw new NotImplementedException();
    }

    public bool EndsInDirectorySeparator(string path) {
      throw new NotImplementedException();
    }

    public bool Exists([NotNullWhen(true)] string? path) {
      throw new NotImplementedException();
    }

    public ReadOnlySpan<char> GetDirectoryName(ReadOnlySpan<char> path) {
      throw new NotImplementedException();
    }

    public string? GetDirectoryName(string? path) {
      throw new NotImplementedException();
    }

    public ReadOnlySpan<char> GetExtension(ReadOnlySpan<char> path) {
      throw new NotImplementedException();
    }

    [return: NotNullIfNotNull("path")]
    public string? GetExtension(string? path) {
      throw new NotImplementedException();
    }

    public ReadOnlySpan<char> GetFileName(ReadOnlySpan<char> path) {
      throw new NotImplementedException();
    }

    [return: NotNullIfNotNull("path")]
    public string? GetFileName(string? path) {
      throw new NotImplementedException();
    }

    public ReadOnlySpan<char> GetFileNameWithoutExtension(
        ReadOnlySpan<char> path) {
      throw new NotImplementedException();
    }

    [return: NotNullIfNotNull("path")]
    public string? GetFileNameWithoutExtension(string? path) {
      throw new NotImplementedException();
    }

    public string GetFullPath(string path, string basePath) {
      throw new NotImplementedException();
    }

    public char[] GetInvalidFileNameChars() {
      throw new NotImplementedException();
    }

    public ReadOnlySpan<char> GetPathRoot(ReadOnlySpan<char> path) {
      throw new NotImplementedException();
    }

    public string? GetPathRoot(string? path) {
      throw new NotImplementedException();
    }

    public string GetRandomFileName() {
      throw new NotImplementedException();
    }

    public string GetRelativePath(string relativeTo, string path) {
      throw new NotImplementedException();
    }

    public string GetTempFileName() {
      throw new NotImplementedException();
    }

    public string GetTempPath() {
      throw new NotImplementedException();
    }

    public bool HasExtension(ReadOnlySpan<char> path) {
      throw new NotImplementedException();
    }

    public bool HasExtension([NotNullWhen(true)] string? path) {
      throw new NotImplementedException();
    }

    public bool IsPathFullyQualified(ReadOnlySpan<char> path) {
      throw new NotImplementedException();
    }

    public bool IsPathFullyQualified(string path) {
      throw new NotImplementedException();
    }

    public bool IsPathRooted(ReadOnlySpan<char> path) {
      throw new NotImplementedException();
    }

    public bool IsPathRooted(string? path) {
      throw new NotImplementedException();
    }

    public string Join(ReadOnlySpan<char> path1, ReadOnlySpan<char> path2) {
      throw new NotImplementedException();
    }

    public string Join(ReadOnlySpan<char> path1,
                       ReadOnlySpan<char> path2,
                       ReadOnlySpan<char> path3) {
      throw new NotImplementedException();
    }

    public string Join(ReadOnlySpan<char> path1,
                       ReadOnlySpan<char> path2,
                       ReadOnlySpan<char> path3,
                       ReadOnlySpan<char> path4) {
      throw new NotImplementedException();
    }

    public string Join(string? path1, string? path2) {
      throw new NotImplementedException();
    }

    public string Join(string? path1, string? path2, string? path3) {
      throw new NotImplementedException();
    }

    public string Join(string? path1,
                       string? path2,
                       string? path3,
                       string? path4) {
      throw new NotImplementedException();
    }

    public string Join(params string?[] paths) {
      throw new NotImplementedException();
    }

    public string Join(params ReadOnlySpan<string?> paths) {
      throw new NotImplementedException();
    }

    public ReadOnlySpan<char> TrimEndingDirectorySeparator(
        ReadOnlySpan<char> path) {
      throw new NotImplementedException();
    }

    public string TrimEndingDirectorySeparator(string path) {
      throw new NotImplementedException();
    }

    public bool TryJoin(ReadOnlySpan<char> path1,
                        ReadOnlySpan<char> path2,
                        Span<char> destination,
                        out int charsWritten) {
      throw new NotImplementedException();
    }

    public bool TryJoin(ReadOnlySpan<char> path1,
                        ReadOnlySpan<char> path2,
                        ReadOnlySpan<char> path3,
                        Span<char> destination,
                        out int charsWritten) {
      throw new NotImplementedException();
    }

    public char AltDirectorySeparatorChar { get; }
    public char DirectorySeparatorChar { get; }
    public char PathSeparator { get; }
    public char VolumeSeparatorChar { get; }
  }
}