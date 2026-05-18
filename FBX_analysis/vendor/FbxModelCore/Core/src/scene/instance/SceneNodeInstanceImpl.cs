using System;
using System.Collections.Generic;
using System.Linq;

using fin.math.rotations;
using fin.math.transform;

namespace fin.scene.instance;

public partial class SceneInstanceImpl {
  private sealed class SceneNodeInstanceImpl
      : ISceneNodeInstance {
    private readonly IReadOnlySceneNode sceneObject_;
    public SceneNodeInstanceImpl(SceneNodeInstanceImpl? parent,
                                 IReadOnlySceneNode sceneObject) {
      this.sceneObject_ = sceneObject;
      
      this.Transform = new Transform3d(parent?.Transform);
      this.Transform.LocalTranslation = sceneObject.Position;
      this.Transform.LocalRotation = QuaternionUtil.Create(sceneObject.Rotation);
      this.Transform.LocalScale = sceneObject.Scale;

      this.ChildNodes = sceneObject.ChildNodes
                                   .Select(n => new SceneNodeInstanceImpl(
                                               this,
                                               n))
                                   .ToArray();
    }

    ~SceneNodeInstanceImpl() => this.ReleaseUnmanagedResources_();

    public void Dispose() {
      this.ReleaseUnmanagedResources_();
      GC.SuppressFinalize(this);
    }

    private void ReleaseUnmanagedResources_() {
      foreach (var child in this.ChildNodes) {
        child.Dispose();
      }
    }

    public IReadOnlySceneNode Definition => this.sceneObject_;

    public IReadOnlyList<ISceneNodeInstance> ChildNodes { get; }

    public Transform3d Transform { get; }

    public void Tick() {
      foreach (var component in this.sceneObject_.Components) {
        if (component is ISceneNodeTickComponent tickComponent) {
          tickComponent.Tick(this);
        }
      }

      foreach (var child in this.ChildNodes) {
        child.Tick();
      }
    }
  }
}