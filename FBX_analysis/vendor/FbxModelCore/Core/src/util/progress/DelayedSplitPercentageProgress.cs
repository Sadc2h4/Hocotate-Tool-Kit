using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace fin.util.progress;

public sealed class DelayedSplitPercentageProgress(int capacity = 0)
    : IPercentageProgress {
  private bool isComplete_;
  private readonly List<PercentageProgress> progresses_ = new(capacity);
  private readonly List<bool> eachIsComplete_ = [];

  public PercentageProgress this[int index] => this.progresses_[index];

  private readonly object updateLock_ = new();

  public PercentageProgress Add() {
    lock (this.updateLock_) {
      var currentIndex = this.progresses_.Count;

      var progress = new PercentageProgress();
      this.progresses_.Add(progress);
      this.eachIsComplete_.Add(false);

      progress.OnProgressChanged += (_, _) => this.ReportProgressChanged_();
      progress.OnComplete += (_, _) => this.ReportComplete_(currentIndex);

      return progress;
    }
  }

  private void ReportProgressChanged_() {
    if (this.isComplete_) {
      return;
    }

    lock (this.updateLock_) {
      var totalProgress = 0f;
      foreach (var (progress, isComplete) in this.progresses_.Zip(
                   this.eachIsComplete_)) {
        totalProgress += !isComplete ? progress.Progress : 1;
      }

      this.Progress = totalProgress / this.progresses_.Count;
      this.OnProgressChanged?.Invoke(this, this.Progress);
    }
  }

  private void ReportComplete_(int index) {
    if (this.isComplete_) {
      return;
    }

    lock (this.updateLock_) {
      this.eachIsComplete_[index] = true;
      this.isComplete_ = this.eachIsComplete_.All(b => b);
      if (!this.isComplete_) {
        this.ReportProgressChanged_();
      } else {
        this.OnComplete?.Invoke(this, EventArgs.Empty);
      }
    }
  }

  public float Progress { get; private set; }
  public event EventHandler<float>? OnProgressChanged;
  public event EventHandler? OnComplete;
}