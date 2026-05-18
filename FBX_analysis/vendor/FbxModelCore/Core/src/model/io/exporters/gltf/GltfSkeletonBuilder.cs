using System.Linq;

using fin.data.queues;

using SharpGLTF.Schema2;

namespace fin.model.io.exporters.gltf;

using GltfNode = Node;
using GltfSkin = Skin;

public sealed class GltfSkeletonBuilder {
  public static (GltfNode, IReadOnlyBone)[] BuildAndBindSkeleton(
      GltfNode rootNode,
      GltfSkin skin,
      float scale,
      IReadOnlySkeleton skeleton) {
    var rootBone = skeleton.Root;

    var boneQueue
        = new FinQueue<(GltfNode, IReadOnlyBone)>((rootNode, rootBone));

    var skinNodesAndBones = new (GltfNode, IReadOnlyBone)[skeleton.Bones.Count - 1];
    while (boneQueue.Count > 0) {
      var (node, bone) = boneQueue.Dequeue();

      ApplyBoneOrientationToNode_(node, bone, scale);

      if (bone != rootBone) {
        skinNodesAndBones[bone.Index - 1] = (node, bone);
      }

      boneQueue.Enqueue(
          bone.Children.Select(child => (
                                   node.CreateNode(child.Name), child)));
    }

    var skinNodes = skinNodesAndBones
                    .Select(skinNodesAndBone => skinNodesAndBone.Item1)
                    .ToArray();
    if (skinNodes.Length > 0) {
      skin.BindJoints(skinNodes);
    } else {
      var nullNode = rootNode.CreateNode("null");
      skin.BindJoints(nullNode);
      skinNodesAndBones = [(nullNode, null)];
    }

    return skinNodesAndBones.ToArray();
  }

  private static void ApplyBoneOrientationToNode_(GltfNode node,
                                                  IReadOnlyBone bone,
                                                  float scale) {
    var matrix = bone.Transform.LocalMatrix;
    matrix.Translation *= scale;
    node.LocalMatrix = matrix;
  }
}