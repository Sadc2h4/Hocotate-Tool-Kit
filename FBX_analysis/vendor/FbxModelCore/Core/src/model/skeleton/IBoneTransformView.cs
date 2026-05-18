using System.Numerics;

namespace fin.model.skeleton;

public interface IBoneTransformView {
  void TargetBone(IReadOnlyBone bone);

  bool TryGetWorldTranslation(out Vector3 translation);
  bool TryGetWorldRotation(out Quaternion rotation);
  bool TryGetWorldScale(out Vector3 scale);

  bool TryGetLocalTranslation(out Vector3 translation);
  bool TryGetLocalRotation(out Quaternion rotation);
  bool TryGetLocalScale(out Vector3 scale);
}