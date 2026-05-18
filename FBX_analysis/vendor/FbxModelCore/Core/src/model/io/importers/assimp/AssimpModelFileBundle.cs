using fin.io;

namespace fin.model.io.importers.assimp;

public sealed class AssimpModelFileBundle : IModelFileBundle {
  public required IReadOnlyTreeFile MainFile { get; init; }
}