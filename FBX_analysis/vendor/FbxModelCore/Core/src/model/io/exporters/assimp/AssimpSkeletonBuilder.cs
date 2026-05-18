using System.Collections.Generic;

using Assimp;

using fin.model;

namespace fin.exporter.assimp;

public class AssimpSkeletonBuilder {
  public void BuildAndBindSkeleton(
      Scene assScene,
      IReadOnlyModel finModel) {
    var finRootBone = finModel.Skeleton.Root;

    var assRootNode = assScene.RootNode;

    var boneQueue = new Queue<(Node, IReadOnlyBone)>();
    boneQueue.Enqueue((assRootNode, finRootBone));

    while (boneQueue.Count > 0) {
      var (assNode, finBone) = boneQueue.Dequeue();

      assNode.Transform = finBone.Transform.LocalMatrix;

      foreach (var childFinBone in finBone.Children) {
        var childAssNode = new Node(childFinBone.Name);
        assNode.Children.Add(childAssNode);

        boneQueue.Enqueue((childAssNode, childFinBone));
      }
    }
  }
}