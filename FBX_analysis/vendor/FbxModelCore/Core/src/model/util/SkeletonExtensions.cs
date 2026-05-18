using System;
using System.Numerics;

using fin.math.matrix.four;
using fin.math.rotations;
using fin.math.xyz;
using fin.ui;


namespace fin.model.util;

public static class SkeletonExtensions {
  public static IBone AddChild(this IBone parent, Vector3 position)
    => parent.AddChild(position.X, position.Y, position.Z);

  public static IBone AddChild(this IBone parent, IReadOnlyXyz position)
    => parent.AddChild(position.X, position.Y, position.Z);

  public static IBone AddChild(this IBone parent,
                               IReadOnlyFinMatrix4x4 matrix)
    => parent.AddChild(matrix.Impl);

  public static IBone AddChild(this IBone parent, Matrix4x4 matrix) {
    matrix.AssertDecompose(out var translation,
                           out var rotation,
                           out var scale);

    var child = parent.AddChild(translation);
    child.Transform.LocalRotation = rotation;
    child.Transform.LocalScale = scale;

    return child;
  }

  public static bool TryGetFaceCameraQuaternion(
      this IReadOnlyBone? bone,
      out Quaternion rotation) {
    var faceTowardsCameraType
        = bone?.FaceTowardsCameraType ?? FaceTowardsCameraType.NONE;
    if (faceTowardsCameraType != FaceTowardsCameraType.NONE) {
      var camera = Camera.Instance;

      rotation = bone?.FaceTowardsCameraPreAdjustment ?? Quaternion.Identity;
      if (faceTowardsCameraType is FaceTowardsCameraType.YAW_AND_PITCH) {
        rotation *= Quaternion.CreateFromAxisAngle(
            Vector3.UnitZ,
            camera.YawDegrees * FinTrig.DEG_2_RAD - MathF.PI / 2);
        rotation *= Quaternion.CreateFromAxisAngle(
            Vector3.UnitX,
            camera.PitchDegrees * FinTrig.DEG_2_RAD + MathF.PI / 2);
      } else {
        rotation *= Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI / 2);
        rotation *= Quaternion.CreateFromAxisAngle(
            Vector3.UnitZ,
            camera.YawDegrees * FinTrig.DEG_2_RAD - MathF.PI / 2);
      }
      rotation *= bone?.FaceTowardsCameraPostAdjustment ?? Quaternion.Identity;

      return true;
    }

    rotation = default;
    return false;
  }
}