using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace fin.scene.instance;

public partial class SceneInstanceImpl {
  private sealed class SceneAreaInstanceImpl(IReadOnlySceneArea sceneArea)
      : ISceneAreaInstance {
    ~SceneAreaInstanceImpl() => this.ReleaseUnmanagedResources_();

    public void Dispose() {
      this.ReleaseUnmanagedResources_();
      GC.SuppressFinalize(this);
    }

    private void ReleaseUnmanagedResources_() {
      this.CustomSkyboxObject?.Dispose();
      foreach (var obj in this.RootNodes) {
        obj.Dispose();
      }
    }

    public IReadOnlySceneArea Definition => sceneArea;

    public IReadOnlyList<ISceneNodeInstance> RootNodes { get; } = sceneArea
        .RootNodes
        .Select(o => new SceneNodeInstanceImpl(null, o))
        .ToArray();

    public void Tick() {
      foreach (var node in this.RootNodes) {
        node.Tick();
      }
    }

    public Color? BackgroundColor => sceneArea.BackgroundColor;

    public ISceneNodeInstance? CustomSkyboxObject { get; }
      = sceneArea.CustomSkyboxNode != null
          ? new SceneNodeInstanceImpl(null, sceneArea.CustomSkyboxNode)
          : null;
  }
}