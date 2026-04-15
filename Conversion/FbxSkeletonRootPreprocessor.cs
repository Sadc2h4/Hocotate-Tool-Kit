using System;
using System.IO;
using Assimp;
using Assimp.Unmanaged;

namespace RARCToolkit.Conversion
{
    internal static class FbxSkeletonRootPreprocessor
    {
        public static string? PrepareInputForSuperBmd(string inputFbx)
        {
            EnsureAssimpLibraryLoaded();

            using var context = new AssimpContext();
            var scene = context.ImportFile(inputFbx);
            if (scene == null)
                return null;

            if (!SceneUsesBones(scene) || ContainsSkeletonRoot(scene.RootNode))
                return null;

            var newRoot = new Node("skeleton_root");
            newRoot.Children.Add(scene.RootNode);
            scene.RootNode = newRoot;

            string sourceDir = Path.GetDirectoryName(inputFbx) ?? ".";
            string tempDae = Path.Combine(
                sourceDir,
                Path.GetFileNameWithoutExtension(inputFbx) + ".__htk_skeleton_root__.dae");
            context.ExportFile(scene, tempDae, "collada");
            return tempDae;
        }

        private static void EnsureAssimpLibraryLoaded()
        {
            if (AssimpLibrary.Instance.IsLibraryLoaded)
                return;

            string baseDir = AppContext.BaseDirectory;
            string[] candidates =
            {
                Path.Combine(baseDir, "resource", "Assimp64.dll"),
                Path.Combine(baseDir, "resource", "assimp.dll"),
                Path.Combine(baseDir, "Assimp64.dll"),
                Path.Combine(baseDir, "assimp.dll"),
            };

            foreach (string candidate in candidates)
            {
                if (File.Exists(candidate))
                {
                    AssimpLibrary.Instance.LoadLibrary(candidate);
                    return;
                }
            }

            throw new FileNotFoundException(
                "Assimp native library not found.\n" +
                "Expected one of: resource\\Assimp64.dll, resource\\assimp.dll");
        }

        private static bool SceneUsesBones(Scene scene)
        {
            foreach (var mesh in scene.Meshes)
            {
                if (mesh.HasBones)
                    return true;
            }

            return false;
        }

        private static bool ContainsSkeletonRoot(Node? node)
        {
            if (node == null)
                return false;

            if (string.Equals(node.Name, "skeleton_root", StringComparison.OrdinalIgnoreCase))
                return true;

            foreach (var child in node.Children)
            {
                if (ContainsSkeletonRoot(child))
                    return true;
            }

            return false;
        }
    }
}
