using System.Collections.Generic;
using System.Numerics;


namespace fin.language.equations.fixedFunction;

public interface IFixedFunctionRegisters {
  IReadOnlyList<IColorRegister> ColorRegisters { get; }
  IReadOnlyList<IScalarRegister> ScalarRegisters { get; }

  IColorRegister GetOrCreateColorRegister(
      string name,
      IColorConstant defaultValue);

  IScalarRegister GetOrCreateScalarRegister(
      string name,
      IScalarConstant defaultValue);
}

public interface IColorRegister : IColorNamedValue {
  IColorConstant DefaultValue { get; set; }
  Vector3 Value { get; set; }
}

public interface IScalarRegister : IScalarNamedValue {
  IScalarConstant DefaultValue { get; set; }
  float Value { get; set; }
}