using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

using CommunityToolkit.Diagnostics;


namespace fin.math.splines;

public sealed class LinearSpline {
  private readonly IReadOnlyList<Vector3> positions_;
  private readonly IReadOnlyList<float> segmentLengths_;
  private readonly float totalLength_;

  public LinearSpline(IReadOnlyList<Vector3> positions) {
    this.positions_ = positions;

    var segmentLengths = new float[positions.Count - 1];
    this.segmentLengths_ = segmentLengths;

    Vector3 previous = positions[0];
    for (var i = 1; i < positions.Count; i++) {
      var current = positions[i];

      segmentLengths[i - 1] = (current - previous).Length();

      previous = current;
    }

    this.totalLength_ = this.segmentLengths_.Sum();
  }

  public Vector3 GetTranslationAtOffset(float position, float xOffset) {
    Guard.IsGreaterThanOrEqualTo(position, 0);

    position %= this.totalLength_;

    Vector3 previous = this.positions_[0];
    for (var i = 1; i < this.positions_.Count; i++) {
      var current = this.positions_[i];

      var distance = this.segmentLengths_[i - 1];
      if (position < distance) {
        var fraction = position / distance;

        var forwardNormal = Vector3.Normalize(current - previous);
        var upNormal = Vector3.UnitY;
        var rightNormal = Vector3.Cross(forwardNormal, upNormal);

        var translation = Vector3.Lerp(previous, current, fraction);
        translation += rightNormal * xOffset;

        return translation;
      }

      position -= distance;
      previous = current;
    }

    throw new InvalidDataException();
  }
}