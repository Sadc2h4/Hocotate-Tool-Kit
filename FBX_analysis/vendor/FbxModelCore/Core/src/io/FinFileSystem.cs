using System.IO.Abstractions;

using fin.io.filesystem;

using IoDirectory = System.IO.Abstractions.IDirectory;
using IoFile = System.IO.Abstractions.IFile;

namespace fin.io;

public static class FinFileSystem {
  public static IFileSystem FileSystem { get; set; } = new ComplexFileSystem();

  public static IoFile File => FileSystem.File;
  public static IoDirectory Directory => FileSystem.Directory;

  public static ISystemDirectory CreateVirtualTempDirectory()
    => new FinDirectory(Directory.CreateTempSubdirectory().FullName);
}