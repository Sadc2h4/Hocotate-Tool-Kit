using System.IO;

namespace fin.io.bundles;

public interface IGameAndLocalPath {
  string GameName { get; }
  string LocalPath { get; }
  string GameAndLocalPath => Path.Join(this.GameName, this.LocalPath);
}