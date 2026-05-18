using fin.model;
using fin.util.enumerables;
using fin.util.strings;

namespace fin.shaders.glsl.source;

public sealed class NullShaderSourceGlsl(
    IReadOnlyModel model,
    IModelRequirements modelRequirements,
    IShaderRequirements shaderRequirements)
    : IShaderSourceGlsl {
  public string VertexShaderSource { get; } =
    GlslUtil.GetVertexSrc(model, modelRequirements, shaderRequirements);

  public string FragmentShaderSource {
    get {
      var sb = new IndentedStringBuilder();
      sb.AppendLine(
          $"""
           #version {GlslConstants.FRAGMENT_SHADER_VERSION}
           {GlslConstants.FLOAT_PRECISION}

           out vec4 fragColor;
           """);

      var hasColors = shaderRequirements.UsedColors.AnyTrue();
      if (hasColors) {
        sb.AppendLine(
            $"""

             in vec4 {GlslConstants.IN_VERTEX_COLOR_NAME}0;
             """);
      }

      sb.AppendLine();
      sb.AppendBlock(
          "void main()",
          () => this.AppendFragmentMain(sb));

      return sb.ToString();
    }
  }

  public void AppendFragmentMain(IndentedStringBuilder sb) {
    sb.AppendLine(
        $"fragColor = {(shaderRequirements.UsedColors.AnyTrue() ? $"{GlslConstants.IN_VERTEX_COLOR_NAME}0" : "vec4(1)")};");
  }
}