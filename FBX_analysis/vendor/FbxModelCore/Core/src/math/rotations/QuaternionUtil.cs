using System;
using System.Numerics;
using System.Runtime.CompilerServices;

using fin.math.floats;
using fin.model;

using Quaternion = System.Numerics.Quaternion;

namespace fin.math.rotations;

public static class QuaternionUtil {
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static Quaternion Create(IRotation rotation)
    => CreateZyxRadians(rotation.XRadians,
                        rotation.YRadians,
                        rotation.ZRadians);

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static Quaternion CreateXyz(
      float xRadians,
      float yRadians,
      float zRadians) {
    return Quaternion.CreateFromAxisAngle(Vector3.UnitX, xRadians) *
           Quaternion.CreateFromAxisAngle(Vector3.UnitY, yRadians) *
           Quaternion.CreateFromAxisAngle(Vector3.UnitZ, zRadians);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static Quaternion CreateZxy(
      float xRadians,
      float yRadians,
      float zRadians) {
    return Quaternion.CreateFromAxisAngle(Vector3.UnitZ, zRadians) *
           Quaternion.CreateFromAxisAngle(Vector3.UnitX, xRadians) *
           Quaternion.CreateFromAxisAngle(Vector3.UnitY, yRadians);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static Quaternion CreateZyxRadians(
      float xRadians,
      float yRadians,
      float zRadians) {
    var cr = FinTrig.Cos(xRadians * 0.5f);
    var sr = FinTrig.Sin(xRadians * 0.5f);
    var cp = FinTrig.Cos(yRadians * 0.5f);
    var sp = FinTrig.Sin(yRadians * 0.5f);
    var cy = FinTrig.Cos(zRadians * 0.5f);
    var sy = FinTrig.Sin(zRadians * 0.5f);

    // Have to round off quaternion values to avoid issues where less
    // significant bits are slightly off and break tests.
    return new Quaternion(sr * cp * cy - cr * sp * sy,
                          cr * sp * cy + sr * cp * sy,
                          cr * cp * sy - sr * sp * cy,
                          cr * cp * cy + sr * sp * sy);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static Quaternion CreateZyxRadians(in this Vector3 xyzRadians)
    => CreateZyxRadians(xyzRadians.X, xyzRadians.Y, xyzRadians.Z);

  // TODO: Slow! Figure out how to populate animations with raw quaternions instead
  public static Vector3 ToEulerRadians(in this Quaternion q) {
    if (q.IsIdentity) {
      return Vector3.Zero;
    }

    Vector3 angles;

    var qy2 = q.Y * q.Y;

    // roll / x
    var sinr_cosp = 2 * (q.W * q.X + q.Y * q.Z);
    var cosr_cosp = 1 - 2 * (q.X * q.X + qy2);
    angles.X = FinTrig.Atan2(sinr_cosp, cosr_cosp);

    // pitch / y
    var sinp = (float) (2 * (q.W * q.Y - q.Z * q.X));
    if (Math.Abs(sinp) >= 1) {
      angles.Y = MathF.CopySign(MathF.PI / 2, sinp);
    } else {
      angles.Y = FinTrig.Asin(sinp);
    }

    // yaw / z
    var siny_cosp = 2 * (q.W * q.Z + q.X * q.Y);
    var cosy_cosp = 1 - 2 * (qy2 + q.Z * q.Z);
    angles.Z = FinTrig.Atan2(siny_cosp, cosy_cosp);

    return angles;
  }

  /// <summary>
  ///   Stupid logic that's necessary because Quaternion.Slerp() gives ever so
  ///   slightly different results on different machines. This makes file
  ///   assertions a fucking nightmare, because you can't just compare all
  ///   bytes anymore--a single bit will be off and fail the test.
  ///
  ///   So in testing, we need to switch to a slower, manual version.
  /// </summary>
  public static SlerpHandler Slerp { get; private set; } = Quaternion.Slerp;

  public delegate Quaternion SlerpHandler(Quaternion from,
                                          Quaternion to,
                                          float fraction);

  /// <summary>
  ///   Permanently switches over to using a slower, more consistent version of
  ///   Quaternion.Slerp that gives identical results across machines.
  ///
  ///   Should only be used in tests.
  /// </summary>
  public static void UseSlowButConsistentSlerp()
    => Slerp = SlowButConsistentSlerp;

  /// <summary>
  ///   Shamelessly stolen from:
  ///   https://github.com/sungiant/abacus/blob/21c620dd287aafd79aa8935a18d92df5f0b28816/source/abacus/src/main/Abacus.Single.cs#L157
  /// </summary>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static Quaternion SlowButConsistentSlerp(
      Quaternion from,
      Quaternion to,
      float fraction) {
    Quaternion slerped;
    var remaining = 1 - fraction;
    var angle = Quaternion.Dot(from, to);
    if (angle < 0) {
      from = -from;
      angle = -angle;
    }

    var theta = FinTrig.Acos(angle);
    var f = remaining;
    var a = fraction;

    if (theta > 0.000001f) {
      var x = FinTrig.Sin(remaining * theta);
      var y = FinTrig.Sin(fraction * theta);
      var z = FinTrig.Sin(theta);
      f = x / z;
      a = y / z;
    }

    slerped = new Quaternion(
        (f * from.X) + (a * to.X),
        (f * from.Y) + (a * to.Y),
        (f * from.Z) + (a * to.Z),
        (f * from.W) + (a * to.W));

    return slerped;
  }

  public static Quaternion SlerpTowards(in this Quaternion p,
                                        in Quaternion q,
                                        float t,
                                        bool shortWay) {
    float dot = Quaternion.Dot(p, q).Clamp(-1, 1);
    if (shortWay) {
      if (dot < 0.0f) {
        return SlerpTowards(-p, q, t, true);
      }
    }

    float angle = MathF.Acos(dot);
    if (angle.IsRoughly0()) {
      return q;
    }

    var first = p * MathF.Sin((1f - t) * angle);
    var second = q * MathF.Sin((t) * angle);
    float division = 1f / MathF.Sin(angle);

    return Quaternion.Normalize((first + second) * division);
  }
}