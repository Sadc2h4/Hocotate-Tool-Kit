using System;
using System.IO;

using fin.io;

using SharpGLTF.Schema2;
using SharpGLTF.Validation;

namespace fin.model.io.exporters.gltf;

public static class GltfExporterUtil {
  public static void Export(this ModelRoot modelRoot,
                            string outputFilePath,
                            bool useEmbeddedTextures,
                            bool isLowLevel) {
    var outputDirectoryPath
        = FinIoStatic.GetParentFullName(outputFilePath).ToString();
    var writeContext = WriteContext.Create(
        (path, bytes) => FinFileSystem.File.WriteAllBytes(
            Path.Join(outputDirectoryPath, path),
            bytes),
        (path) => FinFileSystem.File.Create(
            Path.Join(outputDirectoryPath, path)));


    var writeSettings = CreateWriteSettings(
        outputDirectoryPath,
        useEmbeddedTextures,
        isLowLevel);
    writeSettings.CopyTo(writeContext);

    var name = Path.GetFileName(outputFilePath);
    if (FinFileStatic.GetExtension(outputFilePath)
                     .Equals(".glb", StringComparison.OrdinalIgnoreCase)) {
      writeContext.WithBinarySettings();
      writeContext.WriteBinarySchema2(name, modelRoot);
    } else {
      writeContext.WithTextSettings();
      writeContext.WriteTextSchema2(name, modelRoot);
    }
  }

  public static WriteSettings CreateWriteSettings(
      string outputDirectoryPath,
      bool useEmbeddedTextures,
      bool isLowLevel) {
    var writeSettings = new WriteSettings();

    writeSettings.ImageWriting = useEmbeddedTextures
        ? ResourceWriteMode.EmbeddedAsBase64
        : ResourceWriteMode.SatelliteFile;

    if (isLowLevel) {
      writeSettings.MergeBuffers = false;
      writeSettings.Validation = ValidationMode.Skip;
    }

    return writeSettings;
  }
}