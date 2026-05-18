using System.Collections.Generic;
using System.Numerics;

using fin.model;
using fin.model.impl;

namespace fin.scene;

public partial class SceneImpl {
  private sealed class SceneNodeImpl : ISceneNode {
    private readonly List<ISceneNode> childNodes_ = [];
    private readonly List<ISceneNodeComponent> components_ = [];

    public void Dispose() {
      foreach (var component in this.components_) {
        component.Dispose();
      }

      foreach (var childNode in this.childNodes_) {
        childNode.Dispose();
      }
    }

    public string? Name { get; set; }

    public IReadOnlyList<ISceneNode> ChildNodes => this.childNodes_;

    public ISceneNode AddChildNode() {
      var childNode = new SceneNodeImpl();
      this.childNodes_.Add(childNode);
      return childNode;
    }

    public Vector3 Position { get; private set; }
    public IRotation Rotation { get; } = new RotationImpl();
    public Vector3 Scale { get; private set; } = new Vector3(1, 1, 1);

    public ISceneNode SetPosition(float x, float y, float z) {
      this.Position = new Vector3(x, y, z);
      return this;
    }

    public ISceneNode SetRotationRadians(float xRadians,
                                           float yRadians,
                                           float zRadians) {
      this.Rotation.SetRadians(
          xRadians,
          yRadians,
          zRadians
      );
      return this;
    }

    public ISceneNode SetRotationDegrees(float xDegrees,
                                           float yDegrees,
                                           float zDegrees) {
      this.Rotation.SetDegrees(
          xDegrees,
          yDegrees,
          zDegrees
      );
      return this;
    }

    public ISceneNode SetScale(float x, float y, float z) {
      this.Scale = new Vector3(x, y, z);
      return this;
    }

    public IReadOnlyList<ISceneNodeComponent> Components => this.components_;

    public ISceneNode AddComponent(ISceneNodeComponent component) {
      this.components_.Add(component);
      return this;
    }
  }
}