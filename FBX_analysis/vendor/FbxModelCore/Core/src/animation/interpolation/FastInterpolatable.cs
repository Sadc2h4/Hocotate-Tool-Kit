namespace fin.animation.interpolation;

public interface IFastInterpolatable<T> {
  bool TryGetAtFrame(float frame, out T value);
}

public static class FastInterpolatable {
  public static IFastInterpolatable<T> From<T>(IInterpolatable<T> impl) {
    var frames = new T[impl.AnimationLength];
    impl.GetAllFrames(frames);
    return new MemoryInterpolatable<T>(impl.Looping, frames);
  }

  private sealed class MemoryInterpolatable<T>(bool looping, T[] frames)
      : IFastInterpolatable<T> {
    public bool TryGetAtFrame(float frame, out T value) {
      if (looping) {
        frame %= frames.Length;
      }

      value = frames[(int) frame];
      return true;
    }
  }
}