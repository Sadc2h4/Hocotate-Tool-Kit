using System.Collections.Generic;
using System.Drawing;

namespace fin.model.impl;

public partial class ModelImpl<TVertex> {
  private partial class MaterialManagerImpl {
    public IStandardMaterial AddStandardMaterial() {
      var material = new StandardMaterialImpl {
          Index = this.materials_.Count
      };
      this.materials_.Add(material);
      return material;
    }
  }

  private sealed class StandardMaterialImpl : BMaterialImpl, IStandardMaterial {
    public override IEnumerable<IReadOnlyTexture> Textures {
      get {
        if (this.DiffuseTexture != null) {
          yield return this.DiffuseTexture;
        }

        if (this.MaskTexture != null) {
          yield return this.MaskTexture;
        }

        if (this.AmbientOcclusionTexture != null) {
          yield return this.AmbientOcclusionTexture;
        }

        if (this.NormalTexture != null) {
          yield return this.NormalTexture;
        }

        if (this.EmissiveTexture != null) {
          yield return this.EmissiveTexture;
        }

        if (this.SpecularTexture != null) {
          yield return this.SpecularTexture;
        }
      }
    }

    public IReadOnlyTexture? DiffuseTexture { get; set; }
    public Color? DiffuseColor { get; set; }
    public IReadOnlyTexture? MaskTexture { get; set; }
    public IReadOnlyTexture? AmbientOcclusionTexture { get; set; }
    public IReadOnlyTexture? NormalTexture { get; set; }
    public IReadOnlyTexture? EmissiveTexture { get; set; }
    public IReadOnlyTexture? SpecularTexture { get; set; }
  }
}