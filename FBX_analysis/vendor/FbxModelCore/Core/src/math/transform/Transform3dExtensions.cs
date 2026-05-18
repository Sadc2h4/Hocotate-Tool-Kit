using System.Numerics;

using fin.math.matrix.four;
using fin.math.rotations;
using fin.math.xyz;


namespace fin.math.transform;

public static class Transform3dExtensions {
  public static void SetMatrix(this ITransform3d transform,
                               IReadOnlyFinMatrix4x4 matrix)
    => transform.SetMatrix(matrix.Impl);

  public static void SetMatrix(this ITransform3d transform,
                               Matrix4x4 matrix) {
    matrix.AssertDecompose(out var translation,
                           out var quaternion,
                           out var scale);

    transform.LocalTranslation = translation;
    transform.LocalRotation = quaternion;
    transform.LocalScale = scale;
  }


  public static void SetTranslation(this ITransform3d transform,
                                    IReadOnlyXyz xyz)
    => transform.SetTranslation(xyz.X, xyz.Y, xyz.Z);

  public static void SetTranslation(this ITransform3d transform,
                                    float x,
                                    float y,
                                    float z)
    => transform.LocalTranslation = new Vector3(x, y, z);


  public static void SetRotationRadians(this ITransform3d transform,
                                        Vector3 xyz)
    => transform.SetRotationRadians(xyz.X, xyz.Y, xyz.Z);

  public static void SetRotationRadians(this ITransform3d transform,
                                        IReadOnlyXyz xyz)
    => transform.SetRotationRadians(xyz.X, xyz.Y, xyz.Z);

  public static void SetRotationRadians(this ITransform3d transform,
                                        float x,
                                        float y,
                                        float z)
    => transform.SetRotation(QuaternionUtil.CreateZyxRadians(x, y, z));

  public static void SetRotationDegrees(this ITransform3d transform,
                                        Vector3 xyz)
    => transform.SetRotationDegrees(xyz.X, xyz.Y, xyz.Z);

  public static void SetRotationDegrees(this ITransform3d transform,
                                        IReadOnlyXyz xyz)
    => transform.SetRotationDegrees(xyz.X, xyz.Y, xyz.Z);

  public static void SetRotationDegrees(this ITransform3d transform,
                                        float x,
                                        float y,
                                        float z)
    => transform.SetRotationRadians(x * FinTrig.DEG_2_RAD,
                                    y * FinTrig.DEG_2_RAD,
                                    z * FinTrig.DEG_2_RAD);

  public static void SetRotation(this ITransform3d transform,
                                 Quaternion quaternion)
    => transform.LocalRotation = quaternion;

  public static void SetScale(this ITransform3d transform,
                              Vector3 xyz)
    => transform.LocalScale = xyz;

  public static void SetScale(this ITransform3d transform,
                              IReadOnlyXyz xyz)
    => transform.SetScale(xyz.X, xyz.Y, xyz.Z);

  public static void SetScale(this ITransform3d transform,
                              float x,
                              float y,
                              float z)
    => transform.SetScale(new Vector3(x, y, z));
}