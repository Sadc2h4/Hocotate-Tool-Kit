using fin.animation;
using fin.model;
using fin.model.skeleton;

namespace fin.scene.components;

public interface IAnimatableModel {
  IReadOnlyModel Model { get; }
  IBoneTransformManager BoneTransformManager { get; }
  IAnimationPlaybackManager? AnimationPlaybackManager { get; }
  IReadOnlyModelAnimation? Animation { get; set; }
}

public interface IModelRenderComponent
    : ISceneNodeRenderComponent, IAnimatableModel;