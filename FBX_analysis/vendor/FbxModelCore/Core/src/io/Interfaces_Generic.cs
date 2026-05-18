using System.IO;

using readOnly;

namespace fin.io;

[GenerateReadOnly]
public partial interface IGenericFile {
  new string DisplayFullPath { get; }

  [Const]
  new Stream OpenRead();

  Stream OpenWrite();
}