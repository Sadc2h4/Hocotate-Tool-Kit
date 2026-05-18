using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;

namespace fin.model.impl;

public partial class ModelImpl<TVertex> {
  private partial class MaterialManagerImpl {
    public ITextureMaterial AddTextureMaterial(IReadOnlyTexture texture) {
      var material = new TextureMaterialImpl(texture) {
          Index = this.materials_.Count
      };
      this.materials_.Add(material);
      return material;
    }
  }

  private sealed class TextureMaterialImpl(IReadOnlyTexture texture)
      : BMaterialImpl, ITextureMaterial {
    public IReadOnlyTexture Texture { get; } = texture;

    public override IEnumerable<IReadOnlyTexture> Textures { get; }
      = new ReadOnlyCollection<IReadOnlyTexture>([texture]);

    public Color? DiffuseColor { get; set; }
  }
}