using System;
using System.Collections.Generic;

using fin.math.transform;
using fin.model;

using readOnly;

namespace fin.scene;

public interface ITickable {
  void Tick();
}

[GenerateReadOnly]
public partial interface ISceneInstance : ITickable, IDisposable {
  new IReadOnlyScene Definition { get; }

  new IReadOnlyList<ISceneAreaInstance> Areas { get; }

  new IReadOnlyLighting? Lighting { get; }
}

/// <summary>
///   A single area in a scene. This is used to split out the different
///   regions into separate pieces that can be loaded separately; for
///   example, in Ocarina of Time, this is used to represent a single room in
///   a dungeon.
/// </summary>
[GenerateReadOnly]
public partial interface ISceneAreaInstance : ITickable, IDisposable {
  new IReadOnlySceneArea Definition { get; }

  new IReadOnlyList<ISceneNodeInstance> RootNodes { get; }

  new ISceneNodeInstance? CustomSkyboxObject { get; }
}

/// <summary>
///   An instance of an object in a scene. This can be used for anything that
///   appears in the scene, such as the level geometry, scenery, or
///   characters.
/// </summary>
[GenerateReadOnly]
public partial interface ISceneNodeInstance : ITickable, IDisposable {
  new IReadOnlySceneNode Definition { get; }

  new IReadOnlyList<ISceneNodeInstance> ChildNodes { get; }

  new Transform3d Transform { get; }
}