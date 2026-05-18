using fin.animation.interpolation;
using fin.animation.keyframes;
using fin.util.optional;

namespace fin.model.impl;

public partial class ModelImpl<TVertex> {
  private partial class TextureTracksImpl {
    public IStairStepKeyframes<IReadOnlyTexture?> FlipbookSwaps { get; private set; }

    public IStairStepKeyframes<IReadOnlyTexture?> UseFlipbookSwapKeyframes(
        int initialCapacity = 0,
        int? animationLength = null) {
      var transform = texture.TextureTransform;
      var keyframes = new StairStepKeyframes<IReadOnlyTexture?>(
          sharedConfig,
          new IndividualInterpolationConfig<IReadOnlyTexture?> {
              AnimationLength = animationLength,
              InitialCapacity = initialCapacity,
              DefaultValue = Optional.Of(() => (IReadOnlyTexture?) null),
          });

      this.FlipbookSwaps = keyframes;

      return keyframes;
    }
  }
}