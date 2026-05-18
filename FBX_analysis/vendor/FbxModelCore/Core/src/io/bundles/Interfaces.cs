using System;
using System.Collections.Generic;

using fin.util.asserts;
using fin.util.progress;

namespace fin.io.bundles;

public interface IFileBundle : IUiFile, IComparable<IFileBundle> {
  FileBundleType Type { get; }

  IReadOnlyTreeFile MainFile { get; }

  IEnumerable<IReadOnlyGenericFile> Files {
    get { yield return this.MainFile; }
  }

  IReadOnlyTreeDirectory Directory => this.MainFile.AssertGetParent();

  ReadOnlySpan<char> IUiFile.RawName
    => FinIoStatic.GetName(this.DisplayFullPath);

  ReadOnlySpan<char> DisplayName => this.HumanReadableName ?? this.RawName;

  ReadOnlySpan<char> DisplayFullPath => this.MainFile.DisplayFullPath;

  string TrueFullPath => Asserts.CastNonnull(this.MainFile.FullPath);

  int IComparable<IFileBundle>.CompareTo(IFileBundle? other)
    => this.DisplayFullPath.CompareTo(
        other!.DisplayFullPath,
        StringComparison.OrdinalIgnoreCase);
}

public interface INamedAnnotatedFileBundleGatherer
    : IAnnotatedFileBundleGatherer {
  string Name { get; }
}

public interface IAnnotatedFileBundleGatherer {
  void GatherFileBundles(IFileBundleOrganizer organizer,
                         IMutablePercentageProgress mutablePercentageProgress);
}

public interface IAnnotatedFileBundleGathererAccumulator<out TSelf>
    : IAnnotatedFileBundleGatherer
    where TSelf : IAnnotatedFileBundleGathererAccumulator<TSelf> {
  TSelf Add(IAnnotatedFileBundleGatherer gatherer);
  TSelf Add(Action<IFileBundleOrganizer, IMutablePercentageProgress> handler);
  TSelf Add(Action<IFileBundleOrganizer> handler);

  TSelf Add(IAnnotatedFileBundleGatherer gatherer,
            out IPercentageProgress progress);

  TSelf Add(Action<IFileBundleOrganizer, IMutablePercentageProgress> handler,
            out IPercentageProgress progress);

  TSelf Add(Action<IFileBundleOrganizer> handler,
            out IPercentageProgress progress);
}

public interface IAnnotatedFileBundleGathererAccumulatorWithInput<
    out T, out TSelf>
    : IAnnotatedFileBundleGathererAccumulator<TSelf>
    where TSelf : IAnnotatedFileBundleGathererAccumulatorWithInput<T, TSelf> {
  TSelf Add(Action<IFileBundleOrganizer, T> handler);

  TSelf Add(
      Action<IFileBundleOrganizer, IMutablePercentageProgress, T> handler);
}