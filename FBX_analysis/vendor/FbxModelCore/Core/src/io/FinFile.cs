using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

using fin.util.asserts;

namespace fin.io;

public sealed class FinFile(string fullName, FinRootDirectory? root = null)
    : ISystemFile {
  public FinFile(IReadOnlyTreeFile treeFile) : this(treeFile.FullPath) { }

  public override string ToString() => this.DisplayFullPath;


  // Equality
  public override int GetHashCode()
    => string.GetHashCode(this.FullPath, StringComparison.OrdinalIgnoreCase);

  public override bool Equals(object? other) {
    if (ReferenceEquals(this, other)) {
      return true;
    }

    if (other is not ISystemIoObject otherSelf) {
      return false;
    }

    return this.Equals(otherSelf);
  }

  public bool Equals(ISystemIoObject? other)
    => this.Equals(other as IReadOnlyTreeIoObject);

  public bool Equals(IReadOnlySystemIoObject? other)
    => this.Equals(other as IReadOnlyTreeIoObject);

  public bool Equals(IReadOnlyTreeIoObject? other)
    => this.FullPath.Equals(other?.FullPath,
                            StringComparison.OrdinalIgnoreCase);


  // File fields
  public ReadOnlySpan<char> Name => FinIoStatic.GetName(this.FullPath);
  public string FullPath { get; } = fullName;


  // Ancestry methods
  public ReadOnlySpan<char> GetParentFullPath()
    => FinIoStatic.GetParentFullName(this.FullPath);

  public ISystemDirectory AssertGetParent() {
    if (this.TryGetParent(out ISystemDirectory parent)) {
      return parent;
    }

    throw new Exception("Expected parent directory to exist!");
  }

  public bool TryGetParent(out ISystemDirectory parent) {
    var parentName = this.GetParentFullPath();
    if (!parentName.IsEmpty &&
        !(root != null && parentName.Length < root.Impl.FullPath.Length)) {
      parent = new FinDirectory(parentName.ToString(), root);
      return true;
    }

    parent = null;
    return false;
  }

  public IEnumerable<ISystemDirectory> GetAncestry()
    => this.GetUpwardAncestry_().Reverse();

  private IEnumerable<ISystemDirectory> GetUpwardAncestry_() {
    if (!this.TryGetParent(out ISystemDirectory firstParent)) {
      yield break;
    }

    var current = firstParent;
    while (current.TryGetParent(out var parent)) {
      yield return parent;
      current = parent;
    }
  }


  // File methods
  public bool Exists => FinFileStatic.Exists(this.FullPath);

  public string DisplayFullPath
    => root != null ? root.GetDisplayName(this) : this.FullPath;

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public bool Delete() => FinFileStatic.Delete(this.FullPath);

  public string FileType => FinFileStatic.GetExtension(this.FullPath);

  public string FullNameWithoutExtension
    => FinFileStatic.GetNameWithoutExtension(this.FullPath).ToString();

  public ReadOnlySpan<char> NameWithoutExtension
    => FinFileStatic.GetNameWithoutExtension(this.Name);

  public ISystemFile CloneWithFileType(string newExtension) {
    Asserts.True(newExtension.StartsWith("."),
                 $"'{newExtension}' is not a valid extension!");
    return new FinFile(this.FullNameWithoutExtension + newExtension);
  }


  // Read methods
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public Stream OpenRead() => FinFileStatic.OpenRead(this.FullPath);

  // Write methods
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public Stream OpenWrite() => FinFileStatic.OpenWrite(this.FullPath);

  public bool HasRoot(out string rootName, out string localPath) {
    if (root != null) {
      rootName = root.Name;
      localPath = this.FullPath[root.Impl.FullPath.Length..];
      return true;
    }

    rootName = localPath = default!;
    return false;
  }
}