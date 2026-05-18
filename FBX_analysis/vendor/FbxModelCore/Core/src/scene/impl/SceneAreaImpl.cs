using System.Collections.Generic;
using System.Drawing;

using fin.image;

namespace fin.scene;

public partial class SceneImpl {
  private sealed class SceneAreaImpl : ISceneArea {
    private readonly List<ISceneNode> objects_ = [];

    public void Dispose() {
      foreach (var obj in this.objects_) {
        obj.Dispose();
      }
    }

    public IReadOnlyList<ISceneNode> RootNodes => this.objects_;

    public ISceneNode AddRootNode() {
      var obj = new SceneNodeImpl();
      this.objects_.Add(obj);
      return obj;
    }

    public Color? BackgroundColor { get; set; }
    public IReadOnlyImage? BackgroundImage { get; set; }
    public float BackgroundImageScale { get; set; } = 1;
    public ISceneNode? CustomSkyboxNode { get; set; }

    public ISceneNode CreateCustomSkyboxNode()
      => this.CustomSkyboxNode = new SceneNodeImpl();
  }
}