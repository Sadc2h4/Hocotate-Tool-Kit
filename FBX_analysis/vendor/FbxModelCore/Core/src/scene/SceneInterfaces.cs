using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;

using fin.image;
using fin.importers;
using fin.io.bundles;
using fin.model;
using fin.util.types;

using readOnly;

namespace fin.scene;

public interface ISceneFileBundle : I3dFileBundle {
  FileBundleType IFileBundle.Type => FileBundleType.SCENE;
}

public interface ISceneImporter<in TSceneFileBundle>
    : I3dImporter<IScene, TSceneFileBundle>
    where TSceneFileBundle : ISceneFileBundle;

// Scene
/// <summary>
///   A single scene from a game. These can be thought of as the parts of the
///   game that are each separated by a loading screen.
/// </summary>
[GenerateReadOnly]
public partial interface IScene : IResource, IDisposable {
  new IReadOnlyList<ISceneArea> Areas { get; }
  ISceneArea AddArea();

  new ILighting? Lighting { get; }
  ILighting CreateLighting();
}

/// <summary>
///   A single area in a scene. This is used to split out the different
///   regions into separate pieces that can be loaded separately; for
///   example, in Ocarina of Time, this is used to represent a single room in
///   a dungeon.
/// </summary>
[GenerateReadOnly]
public partial interface ISceneArea : IDisposable {
  new IReadOnlyList<ISceneNode> RootNodes { get; }
  ISceneNode AddRootNode();

  new Color? BackgroundColor { get; set; }

  new IReadOnlyImage? BackgroundImage { get; set; }
  new float BackgroundImageScale { get; set; }

  new ISceneNode? CustomSkyboxNode { get; set; }
  ISceneNode CreateCustomSkyboxNode();
}

/// <summary>
///   An instance of an object in a scene. This can be used for anything that
///   appears in the scene, such as the level geometry, scenery, or
///   characters.
/// </summary>
[GenerateReadOnly]
public partial interface ISceneNode : IDisposable, INamed {
  new IReadOnlyList<ISceneNode> ChildNodes { get; }
  ISceneNode AddChildNode();

  new Vector3 Position { get; }
  new IRotation Rotation { get; }
  new Vector3 Scale { get; }

  ISceneNode SetPosition(float x, float y, float z);

  ISceneNode SetRotationRadians(float xRadians,
                                  float yRadians,
                                  float zRadians);

  ISceneNode SetRotationDegrees(float xDegrees,
                                  float yDegrees,
                                  float zDegrees);

  ISceneNode SetScale(float x, float y, float z);

  new IReadOnlyList<ISceneNodeComponent> Components { get; }
  ISceneNode AddComponent(ISceneNodeComponent component);
}

[UnionCandidate]
public interface ISceneNodeComponent : IDisposable;

public interface ISceneNodeTickComponent : ISceneNodeComponent {
  void Tick(ISceneNodeInstance self);
}

public interface ISceneNodeRenderComponent : ISceneNodeComponent {
  void Render(ISceneNodeInstance self);
}