using System.Collections.Generic;
using System.Linq;

using fin.data.queues;
using fin.scene.components;
using fin.util.linq;


namespace fin.scene;

public static class SceneInstanceExtensions {
  public static IEnumerable<ISceneNodeInstance> EnumerateAllNodes(
      this ISceneInstance sceneInstance) {
    var queue = new FinQueue<ISceneNodeInstance>(
        sceneInstance.Areas.SelectMany(a => a.RootNodes));
    while (queue.TryDequeue(out var node)) {
      yield return node;
      queue.Enqueue(node.ChildNodes);
    }
  }

  public static IEnumerable<IAnimatableModel> EnumerateAllAnimatableModels(
      this ISceneInstance scene) {
    foreach (var node in scene.EnumerateAllNodes()) {
      foreach (var modelRenderComponent
               in node.Definition.Components
                      .WhereIs<ISceneNodeComponent, IModelRenderComponent>()) {
        yield return modelRenderComponent;
      }
    }
  }
}