using System.Numerics;

using readOnly;

namespace fin.math.transform;

[GenerateReadOnly]
public partial interface ITransform2d : ITransform<Vector2, float, Vector2>;

public sealed class Transform2d : ITransform2d {
  public Vector2 LocalTranslation { get; set; }
  public float LocalRotation { get; set; }
  public Vector2 LocalScale { get; set; } = Vector2.One;
}