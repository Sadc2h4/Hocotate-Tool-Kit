using System.Collections.Generic;
using System.Text;

namespace fin.shaders.glsl.functions;

public enum GlslUniform {
  CAMERA_POSITION,
  SHININESS,
}

public enum GlslVarying {
  VERTEX_NORMAL,
  VERTEX_POSITION,
}

public interface IGlslFunction {
  string Name { get; }
  IEnumerable<GlslUniform> UniformDependencies { get; }
  IEnumerable<GlslVarying> VaryingDependencies { get; }
  IEnumerable<IGlslFunction> FunctionDependencies { get; }
  void AppendSourceToBuilder(StringBuilder sb);
}