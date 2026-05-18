using System.Numerics;

using fin.animation;
using fin.animation.interpolation;
using fin.data.indexable;
using fin.math.interpolation;

namespace fin.model.skeleton;

public sealed class AnimatedBoneTransformView : IBoneTransformView {
  private readonly MagFilterInterpolatable<Vector3>
      translationMagFilterInterpolationTrack_ = new(new Vector3Interpolator()) {
          AnimationInterpolationMagFilter
              = AnimationInterpolationMagFilter.ORIGINAL_FRAME_RATE_LINEAR
      };

  private readonly MagFilterInterpolatable<Quaternion>
      rotationMagFilterInterpolationTrack_ =
          new(new SimpleQuaternionInterpolator()) {
              AnimationInterpolationMagFilter
                  = AnimationInterpolationMagFilter.ORIGINAL_FRAME_RATE_LINEAR
          };

  private readonly MagFilterInterpolatable<Vector3>
      scaleMagFilterInterpolationTrack_ = new(new Vector3Interpolator()) {
          AnimationInterpolationMagFilter
              = AnimationInterpolationMagFilter.ORIGINAL_FRAME_RATE_LINEAR
      };

  public IReadOnlyModelAnimation? Animation { get; set; }
  public IAnimationPlaybackManager? PlaybackManager { get; set; }

  private float Frame => (float) (this.PlaybackManager?.Frame ?? 0);

  public void TargetBone(IReadOnlyBone bone) {
    this.translationMagFilterInterpolationTrack_.Impl = null;
    this.rotationMagFilterInterpolationTrack_.Impl = null;
    this.scaleMagFilterInterpolationTrack_.Impl = null;

    if (this.Animation?.BoneTracks.TryGetValue(bone, out var boneTracks) ??
        false) {
      // Only gets the values from the animation if the frame is at least partially defined.
      if (boneTracks.Translations?.HasAnyData ?? false) {
        this.translationMagFilterInterpolationTrack_.Impl
            = boneTracks.Translations;
      }

      if (boneTracks.Rotations?.HasAnyData ?? false) {
        this.rotationMagFilterInterpolationTrack_.Impl = boneTracks.Rotations;
      }

      if (boneTracks.Scales?.HasAnyData ?? false) {
        this.scaleMagFilterInterpolationTrack_.Impl = boneTracks.Scales;
      }
    }
  }

  public bool TryGetWorldTranslation(out Vector3 translation) {
    translation = default;
    return false;
  }

  public bool TryGetWorldRotation(out Quaternion rotation) {
    rotation = default;
    return false;
  }

  public bool TryGetWorldScale(out Vector3 scale) {
    scale = default;
    return false;
  }

  public bool TryGetLocalTranslation(out Vector3 translation) {
    if (this.translationMagFilterInterpolationTrack_.Impl == null) {
      translation = default;
      return false;
    }

    return this.translationMagFilterInterpolationTrack_
               .TryGetAtFrame(this.Frame, out translation);
  }

  public bool TryGetLocalRotation(out Quaternion rotation) {
    if (this.rotationMagFilterInterpolationTrack_.Impl == null) {
      rotation = default;
      return false;
    }

    return this.rotationMagFilterInterpolationTrack_
               .TryGetAtFrame(this.Frame, out rotation);
  }

  public bool TryGetLocalScale(out Vector3 scale) {
    if (this.scaleMagFilterInterpolationTrack_.Impl == null) {
      scale = default;
      return false;
    }

    return this.scaleMagFilterInterpolationTrack_
               .TryGetAtFrame(this.Frame, out scale);
  }
}