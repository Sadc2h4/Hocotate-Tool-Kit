namespace fin.io;

public sealed record FinRootDirectory(string Name, ISystemDirectory Impl) {
  public string GetLocalPath(ISystemIoObject ioObject)
    => ioObject.FullPath[this.Impl.FullPath.Length..];

  public string GetDisplayName(ISystemIoObject ioObject)
    => $"//{this.Name}{this.GetLocalPath(ioObject).Replace('\\', '/')}";
}