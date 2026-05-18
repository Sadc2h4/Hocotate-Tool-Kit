using System.Collections.Generic;

namespace fin.model.impl;

public partial class ModelImpl<TVertex> {
  private partial class MaterialManagerImpl {
    public IHiddenMaterial AddHiddenMaterial() {
      var material = new HiddenMaterialImpl {
          Index = this.materials_.Count
      };
      this.materials_.Add(material);
      return material;
    }
  }

  private sealed class HiddenMaterialImpl : BMaterialImpl, IHiddenMaterial {
    public override IEnumerable<ITexture> Textures { get; } =
      [];
  }
}