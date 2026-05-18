using System.Collections.Generic;

using fin.data.queues;


namespace fin.model.skin;

public static class MeshExtensions {
  public static IEnumerable<IReadOnlyMesh> SelfAndChildren(
      this IReadOnlyMesh root) {
    var queue = new FinQueue<IReadOnlyMesh>(root);
    while (queue.TryDequeue(out var current)) {
      yield return current;

      queue.Enqueue(current.SubMeshes);
    }
  }
}