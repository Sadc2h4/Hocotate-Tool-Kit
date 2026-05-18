using fin.util.types;

namespace fin.ui.rendering.gl;

[IocCandiate]
public static class OpenGlVersionService {
  public static void Init(bool isOpenGlEs) {
    Es = isOpenGlEs;

    if (isOpenGlEs) {
      MajorVersion = 3;
      MinorVersion = 1;
    } else {
      MajorVersion = 4;
      MinorVersion = 6;
    }
  } 

  public static bool Es { get; private set; }
  public static int MajorVersion { get; private set; }
  public static int MinorVersion { get; private set; }
}