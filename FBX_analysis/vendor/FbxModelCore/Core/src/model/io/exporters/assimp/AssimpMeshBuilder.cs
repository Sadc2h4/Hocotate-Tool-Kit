using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Assimp;

using fin.data.dictionaries;
using fin.data.queues;
using fin.model;


namespace fin.exporter.assimp {
  using FinPrimitiveType = fin.model.PrimitiveType;
  using AssPrimitiveType = Assimp.PrimitiveType;

  public class AssimpMeshBuilder {
    public void BuildAndBindMesh(
        Scene assScene,
        IReadOnlyModel finModel) {
      var meshNode = new Node("meshes");
      assScene.RootNode.Children.Add(meshNode);

      var meshQueue = new FinTuple2Queue<Node, IReadOnlyMesh>(
          finModel.Skin.RootMeshes.Select(m => (assScene.RootNode, m)));
      while (meshQueue.TryDequeue(out var assParentNode, out var finMesh)) {
        var assNode = new Node(finMesh.Name);
        assParentNode.Children.Add(assNode);

        foreach (var finPrimitive in finMesh.Primitives) {
          var assMesh = new Mesh(AssPrimitiveType.Triangle);
        }

        meshQueue.Enqueue(finMesh.SubMeshes.Select(m => (assNode, m)));
      }
    }
  }
}