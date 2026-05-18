using System;

namespace fin.scene.components;

public class LambdaSceneNodeRenderComponent(
    Action<ISceneNodeInstance> handler) : ISceneNodeRenderComponent {
  public void Dispose() { }
  public void Render(ISceneNodeInstance self) => handler(self);
}