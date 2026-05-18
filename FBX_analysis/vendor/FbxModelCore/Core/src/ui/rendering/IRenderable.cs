using System;


namespace fin.ui.rendering;

public interface IRenderable : IDisposable {
  void Render();
}