using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using fin.util.tasks;

using schema.binary;
using schema.binary.attributes;
using schema.util.enumerables;
using schema.util.sequences;

namespace fin.schema.sequences {
  public class PointerSections<TSection>
      : ISequence<PointerSections<TSection>, TSection>
      where TSection : IBinaryConvertible, new() {
    private readonly List<TSection> sections_;

    private readonly List<long> rPointersToSections_;
    private readonly List<TaskCompletionSource<long>> wPointersToSections_;

    public PointerSections() : this(new()) { }

    private PointerSections(List<TSection> sections) {
      this.sections_ = sections;

      this.wPointersToSections_ =
          sections
              .Select(_ => new TaskCompletionSource<long>())
              .ToList();
      this.rPointersToSections_ = sections.Select(_ => 0L).ToList();
    }

    public TSection GetDefault() => new();

    public int Count => this.sections_.Count;

    public void Clear() {
      this.sections_.Clear();
      this.wPointersToSections_.Clear();
    }

    public void ResizeInPlace(int newLength) {
      SequencesUtil.ResizeSequenceInPlace(this.sections_, newLength);
      SequencesUtil.ResizeSequenceInPlace(this.wPointersToSections_, newLength);
    }

    PointerSections<TSection>
        IReadOnlySequence<PointerSections<TSection>, TSection>.
        CloneWithNewLength(int newLength) {
      var additionalLength = Math.Max(this.Count - newLength, 0);
      return new(
          this.sections_.Resized(newLength)
              .Concat(Enumerable.Repeat(new TSection(), additionalLength))
              .ToList());
    }

    [Ignore]
    public TSection this[int index] {
      get => this.sections_[index];
      set => this.sections_[index] = value;
    }

    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

    public IEnumerator<TSection> GetEnumerator()
      => this.sections_.GetEnumerator();


    // Pointer management
    public bool UseAutomaticPointers { get; set; } = true;

    public void SetPointerToSection(int index, long pointer)
      => this.rPointersToSections_[index] = pointer;

    public Task<long> GetPointerToSection(int index)
      => this.wPointersToSections_[index].Task;


    public void Read(IEndianBinaryReader er) {
      if (UseAutomaticPointers) { }

      var count = this.Count;

      for (var i = 0; i < count; ++i) {
        var val1 = this.list1_[i];
        val1.Read(er);
        this.list1_[i] = val1;
      }

      for (var i = 0; i < count; ++i) {
        var val2 = this.list2_[i];
        val2.Read(er);
        this.list2_[i] = val2;
      }
    }

    public void Write(ISubEndianBinaryWriter ew) {
      var count = this.Count;

      for (var i = 0; i < count; ++i) {
        var obj1 = this.list1_[i];
        var obj2 = this.list2_[i];
        obj1.Other = obj2;
        obj2.Other = obj1;

        obj1.WPointerToOther = new TaskCompletionSource<long>();
        obj2.WPointerToOther = new TaskCompletionSource<long>();
      }

      for (var i = 0; i < count; ++i) {
        ew.GetAbsolutePosition().ThenSetResult(this.list2_[i].WPointerToOther);
        this.list1_[i].Write(ew);
      }

      for (var i = 0; i < count; ++i) {
        ew.GetAbsolutePosition().ThenSetResult(this.list1_[i].WPointerToOther);
        this.list2_[i].Write(ew);
      }
    }
  }
}