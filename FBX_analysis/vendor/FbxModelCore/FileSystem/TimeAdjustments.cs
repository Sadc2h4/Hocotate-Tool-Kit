namespace System.IO.Abstractions.TestingHelpers;

/// <summary>
/// Flags indicating which times to adjust for a <see cref="ImaginaryFileData"/>.
/// </summary>
[Flags]
public enum TimeAdjustments {
  /// <summary>
  /// Adjusts no times on the <see cref="ImaginaryFileData"/>
  /// </summary>
  None = 0,

  /// <summary>
  /// Adjusts the <see cref="ImaginaryFileData.CreationTime"/>
  /// </summary>
  CreationTime = 1 << 0,

  /// <summary>
  /// Adjusts the <see cref="ImaginaryFileData.LastAccessTime"/>
  /// </summary>
  LastAccessTime = 1 << 1,

  /// <summary>
  /// Adjusts the <see cref="ImaginaryFileData.LastWriteTime"/>
  /// </summary>
  LastWriteTime = 1 << 2,

  /// <summary>
  /// Adjusts all times on the <see cref="ImaginaryFileData"/>
  /// </summary>
  All = ~0
}