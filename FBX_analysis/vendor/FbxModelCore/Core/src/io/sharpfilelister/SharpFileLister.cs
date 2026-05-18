using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using static fin.io.sharpfilelister.Interop;

namespace fin.io.sharpDirLister;

public sealed class DirectoryInformation {
  public string AbsoluteSubdirPath { get; set; }
  public LinkedList<string> AbsoluteFilePaths { get; } = [];
  public LinkedList<DirectoryInformation> Subdirs { get; } = [];
}

public static class SharpFileLister {
  public const IntPtr INVALID_HANDLE_VALUE = -1;

  //Code based heavily on https://stackoverflow.com/q/47471744
  public static unsafe DirectoryInformation FindNextFilePInvoke(string path) {
    var directoryInfo = new DirectoryInformation { AbsoluteSubdirPath = path };
    var fileList = directoryInfo.AbsoluteFilePaths;
    var directoryList = directoryInfo.Subdirs;

    IntPtr fileSearchHandle = INVALID_HANDLE_VALUE;
    try {
      fileSearchHandle = FindFirstFileWInDirectory_(path, out var findData);
      if (fileSearchHandle != INVALID_HANDLE_VALUE) {
        do {
          var fileName = new ReadOnlySpan<char>(findData.cFileName, 260);
          fileName = fileName[..fileName.IndexOf('\0')];

          if (fileName is "." or "..") {
            continue;
          }

          var attributes = findData.dwFileAttributes;
          var fullPath = @$"{path}\{fileName}";
          if ((attributes & FileAttributes.Directory) == 0) {
            fileList.AddLast(fullPath);
          } else if ((attributes & FileAttributes.ReparsePoint) == 0) {
            directoryList.AddLast(FindNextFilePInvoke(fullPath));
          }
        } while (FindNextFile(fileSearchHandle, out findData));
      }
    } finally {
      if (fileSearchHandle != INVALID_HANDLE_VALUE) {
        FindClose(fileSearchHandle);
      }
    }

    return directoryInfo;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private static unsafe nint FindFirstFileWInDirectory_(
      ReadOnlySpan<char> directoryPath,
      out WIN32_FIND_DATAW findData) {
    Span<char> pathChars = stackalloc char[directoryPath.Length + 3];
    directoryPath.CopyTo(pathChars);
    pathChars[^3] = '\\';
    pathChars[^2] = '*';

    fixed (char* ptr = &MemoryMarshal.GetReference(pathChars)) {
      return FindFirstFileW((IntPtr) ptr, out findData);
    }
  }
}