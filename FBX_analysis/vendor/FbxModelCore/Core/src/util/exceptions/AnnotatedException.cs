using System;
using System.Collections;
using System.Threading.Tasks;

namespace fin.util.exceptions;

public class AnnotatedException(string annotation, Exception impl) : Exception {
  public static void Space(string annotation, Action handler) {
    try {
      handler();
    } catch (Exception e) {
      throw e.Annotate(annotation);
    }
  }

  public static async Task SpaceAsync(string annotation, Func<Task> handler) {
    try {
      await handler();
    } catch (Exception e) {
      throw e.Annotate(annotation);
    }
  }

  public override string Message => $"{annotation}{impl.Message}";

  public override string? StackTrace => impl.StackTrace;
  public override IDictionary Data => impl.Data;
  public override Exception GetBaseException() => impl;

  public override string? Source {
    get => impl.Source;
    set => impl.Source = value;
  }

  public override string? HelpLink {
    get => impl.HelpLink;
    set => impl.HelpLink = value;
  }
}

public static class ExceptionUtils {
  public static AnnotatedException Annotate(this Exception e, string annotation)
    => new(annotation, e);
}