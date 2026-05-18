using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace fin.util.tasks;

public static class FinTask {
  public static Task Run(Func<Task> action)
    => Task.Run(async () => {
      Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
      await action();
    });

  public static Task Run(Action action)
    => Task.Run(() => {
      Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
      action();
    });

  public static Task Run(Func<Task> action, CancellationToken cancellationToken)
    => Task.Run(
        async () => {
          Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
          await action();
        },
        cancellationToken);

  public static Task Run(Action action, CancellationToken cancellationToken)
    => Task.Run(
        () => {
          Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
          action();
        },
        cancellationToken);
}