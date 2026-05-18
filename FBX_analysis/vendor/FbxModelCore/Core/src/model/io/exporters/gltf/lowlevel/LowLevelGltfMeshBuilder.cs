using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using CommunityToolkit.HighPerformance;

using fin.color;
using fin.math.matrix.four;
using fin.model.accessor;
using fin.model.skeleton;
using fin.model.util;
using fin.shaders.glsl;

using SharpGLTF.Schema2;

using FinPrimitiveType = fin.model.PrimitiveType;
using GltfPrimitiveType = SharpGLTF.Schema2.PrimitiveType;


namespace fin.model.io.exporters.gltf.lowlevel;

public static class LowLevelGltfMeshBuilder {
  public static IList<Mesh> BuildAndBindMesh(
      ModelRoot gltfModel,
      IReadOnlyModel model,
      float scale,
      IDictionary<IReadOnlyMaterial, Material> finToTexCoordAndGltfMaterial) {
    var skin = model.Skin;
    var vertexAccessor = ConsistentVertexAccessor.GetAccessorForModel(model);

    var boneTransformManager = new BoneTransformManager();
    boneTransformManager.CalculateStaticMatricesForManualProjection(model);

    var nullMaterial = gltfModel.CreateMaterial("null");
    nullMaterial.DoubleSided = false;
    nullMaterial.WithPBRSpecularGlossiness();

    var points = model.Skin.Vertices;
    var pointsCount = points.Count;

    var modelRequirements = ModelRequirements.FromModel(model);

    var positionView = gltfModel.CreateBufferView(
        4 * 3 * pointsCount,
        0,
        BufferMode.ARRAY_BUFFER);
    var positionSpan = positionView.Content.AsSpan().Cast<byte, Vector3>();

    for (var p = 0; p < pointsCount; ++p) {
      vertexAccessor.Target(points[p]);
      var point = vertexAccessor;

      boneTransformManager.ProjectVertexPositionNormalTangent(
          point,
          out var outPosition,
          out var outNormal,
          out var outTangent);
      positionSpan[p] = outPosition * scale;
    }

    var positionAccessor = gltfModel.CreateAccessor();
    positionAccessor.SetVertexData(
        positionView,
        0,
        pointsCount);

    Accessor? normalAccessor = null;
    if (modelRequirements.HasNormals) {
      var normalView = gltfModel.CreateBufferView(
          4 * 3 * pointsCount,
          0,
          BufferMode.ARRAY_BUFFER);
      var normalSpan = normalView.Content.AsSpan().Cast<byte, Vector3>();

      for (var p = 0; p < pointsCount; ++p) {
        vertexAccessor.Target(points[p]);

        var outNormal = vertexAccessor.LocalNormal.GetValueOrDefault();
        var transformMatrix =
            boneTransformManager.GetTransformMatrix(vertexAccessor);
        if (transformMatrix != null) {
          ProjectionUtil.ProjectNormal(transformMatrix.Impl, ref outNormal);
        }

        normalSpan[p] = outNormal;
      }

      normalAccessor = gltfModel.CreateAccessor();
      normalAccessor.SetVertexData(
          normalView,
          0,
          pointsCount);
    }

    Accessor? tangentAccessor = null;
    if (modelRequirements.HasTangents) {
      var tangentView = gltfModel.CreateBufferView(
          4 * 4 * pointsCount,
          0,
          BufferMode.ARRAY_BUFFER);
      var tangentSpan = tangentView.Content.AsSpan().Cast<byte, Vector4>();

      for (var p = 0; p < pointsCount; ++p) {
        vertexAccessor.Target(points[p]);

        var outTangent = vertexAccessor.LocalTangent.GetValueOrDefault();
        var transformMatrix =
            boneTransformManager.GetTransformMatrix(vertexAccessor);
        if (transformMatrix != null) {
          ProjectionUtil.ProjectTangent(transformMatrix.Impl, ref outTangent);
        }

        tangentSpan[p] = outTangent;
      }

      tangentAccessor = gltfModel.CreateAccessor();
      tangentAccessor.SetVertexData(
          tangentView,
          0,
          pointsCount);
    }

    var gltfMeshes = new List<Mesh>();
    foreach (var finMesh in skin.Meshes) {
      var gltfMesh = gltfModel.CreateMesh(finMesh.Name);

      foreach (var finPrimitive in finMesh.Primitives) {
        Material material;
        if (finPrimitive.Material != null) {
          material = finToTexCoordAndGltfMaterial[finPrimitive.Material];
        } else {
          material = nullMaterial;
        }

        var gltfPrimitive = gltfMesh.CreatePrimitive();
        gltfPrimitive.Material = material;

        gltfPrimitive.SetVertexAccessor("POSITION", positionAccessor);
        if (normalAccessor != null) {
          gltfPrimitive.SetVertexAccessor("NORMAL", normalAccessor);
        }

        if (tangentAccessor != null) {
          gltfPrimitive.SetVertexAccessor("TANGENT", tangentAccessor);
        }

        if (finPrimitive.Type != FinPrimitiveType.QUADS &&
            finPrimitive.Type != FinPrimitiveType.QUAD_STRIP &&
            finPrimitive.VertexOrder == VertexOrder.COUNTER_CLOCKWISE) {
          gltfPrimitive.DrawPrimitiveType = finPrimitive.Type switch {
              FinPrimitiveType.TRIANGLES => GltfPrimitiveType.TRIANGLES,
              FinPrimitiveType.TRIANGLE_STRIP => GltfPrimitiveType
                  .TRIANGLE_STRIP,
              FinPrimitiveType.TRIANGLE_FAN => GltfPrimitiveType.TRIANGLE_FAN,
              FinPrimitiveType.LINES => GltfPrimitiveType.LINES,
              FinPrimitiveType.LINE_STRIP => GltfPrimitiveType.LINE_STRIP,
              FinPrimitiveType.POINTS => GltfPrimitiveType.POINTS,
              _ => throw new ArgumentOutOfRangeException()
          };

          var finPrimitiveVertices = finPrimitive.Vertices;
          gltfPrimitive.SetIndexAccessor(
              CreateIndexAccessor_(
                  gltfModel,
                  finPrimitiveVertices.Select(vertex => vertex.Index)
                                      .ToArray()));
        } else {
          gltfPrimitive.DrawPrimitiveType = GltfPrimitiveType.TRIANGLES;

          var finTriangleVertexIndices =
              finPrimitive.GetOrderedTriangleVertexIndices().ToArray();
          gltfPrimitive.SetIndexAccessor(
              CreateIndexAccessor_(
                  gltfModel,
                  finTriangleVertexIndices));
          break;
        }
      }

      gltfMeshes.Add(gltfMesh);
    }

    // Vertex colors
    for (var colorI = 0; colorI < modelRequirements.NumColors; ++colorI) {
      var colorView = gltfModel.CreateBufferView(
          4 * 4 * pointsCount,
          0,
          BufferMode.ARRAY_BUFFER);
      var colorSpan = colorView.Content.AsSpan().Cast<byte, Vector4>();

      for (var p = 0; p < pointsCount; ++p) {
        vertexAccessor.Target(points[p]);
        var point = vertexAccessor;

        var finColor0 = point.GetColor(0);
        var hasColor0 = finColor0 != null;
        var assColor0 = hasColor0
            ? FinToGltfColor_(
                finColor0)
            : Vector4.One;
        colorSpan[p] = assColor0;
      }

      var colorAccessor = gltfModel.CreateAccessor();
      colorAccessor.SetVertexData(
          colorView,
          0,
          pointsCount,
          DimensionType.VEC4);

      var colorName = $"COLOR_{colorI}";
      foreach (var gltfMesh in gltfMeshes) {
        foreach (var gltfPrimitive in gltfMesh.Primitives) {
          gltfPrimitive.SetVertexAccessor(colorName, colorAccessor);
        }
      }
    }

    // UVs
    for (var uvI = 0; uvI < modelRequirements.NumUvs; ++uvI) {
      var uvView = gltfModel.CreateBufferView(
          2 * sizeof(float) * pointsCount,
          0,
          BufferMode.ARRAY_BUFFER);
      var uvSpan = uvView.Content.AsSpan().Cast<byte, Vector2>();

      for (var p = 0; p < pointsCount; ++p) {
        vertexAccessor.Target(points[p]);
        var point = vertexAccessor;

        var finUv = point.GetUv(0);
        uvSpan[p] = finUv.Value;
      }

      var uvAccessor = gltfModel.CreateAccessor();
      uvAccessor.SetVertexData(
          uvView,
          0,
          pointsCount,
          DimensionType.VEC2);

      var uvName = $"TEXCOORD_{uvI}";
      foreach (var gltfMesh in gltfMeshes) {
        foreach (var gltfPrimitive in gltfMesh.Primitives) {
          gltfPrimitive.SetVertexAccessor(uvName, uvAccessor);
        }
      }
    }

    return gltfMeshes;
  }

  private static Vector4 FinToGltfColor_(IColor? color)
    => new(color?.Rf ?? 1, color?.Gf ?? 0, color?.Bf ?? 1, color?.Af ?? 1);

  private static Accessor CreateIndexAccessor_(
      ModelRoot gltfModelRoot,
      int[] vertexIndices) {
    var maxIndex = vertexIndices.Max();
    int bytesPerIndex = maxIndex switch {
        < byte.MaxValue   => 1,
        < ushort.MaxValue => 2,
        _                 => 4
    };
    var indexEncodingType = bytesPerIndex switch {
        1 => IndexEncodingType.UNSIGNED_BYTE,
        2 => IndexEncodingType.UNSIGNED_SHORT,
        4 => IndexEncodingType.UNSIGNED_INT,
        _ => throw new ArgumentOutOfRangeException()
    };

    var indexView = gltfModelRoot.CreateBufferView(
        bytesPerIndex * vertexIndices.Length,
        0,
        BufferMode.ELEMENT_ARRAY_BUFFER);

    switch (indexEncodingType) {
      case IndexEncodingType.UNSIGNED_BYTE: {
        int i = 0;
        var bSpan = indexView.Content.AsSpan();
        foreach (var v in vertexIndices) {
          bSpan[i++] = (byte) v;
        }

        break;
      }
      case IndexEncodingType.UNSIGNED_SHORT: {
        int i = 0;
        var sSpan = indexView.Content.AsSpan().Cast<byte, ushort>();
        foreach (var v in vertexIndices) {
          sSpan[i++] = (ushort) v;
        }

        break;
      }
      case IndexEncodingType.UNSIGNED_INT: {
        int i = 0;
        var iSpan = indexView.Content.AsSpan().Cast<byte, uint>();
        foreach (var v in vertexIndices) {
          iSpan[i++] = (uint) v;
        }

        break;
      }
      default: throw new ArgumentOutOfRangeException();
    }

    var indexAccessor = gltfModelRoot.CreateAccessor();
    indexAccessor.SetIndexData(indexView,
                               0,
                               vertexIndices.Length,
                               indexEncodingType);

    return indexAccessor;
  }
}