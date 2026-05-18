using System.Collections.Generic;
using System.Drawing;


namespace fin.model.impl;

public partial class ModelImpl<TVertex> {
  private partial class MaterialManagerImpl {
    public IColorMaterial AddColorMaterial(Color color) {
      var material = new ColorMaterialImpl(color) {
          Index = this.materials_.Count
      };
      this.materials_.Add(material);
      return material;
    }
  }

  private sealed class ColorMaterialImpl(Color color) : BMaterialImpl, IColorMaterial {
    public Color Color { get; set; } = color;

    public override IEnumerable<ITexture> Textures => [];
  }
}