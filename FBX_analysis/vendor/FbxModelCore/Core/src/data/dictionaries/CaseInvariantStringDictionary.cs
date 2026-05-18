using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace fin.data.dictionaries;

public sealed class CaseInvariantStringDictionary<T>
    : IFinDictionary<string, T> {
  private readonly Dictionary<string, T> impl_
      = new(StringComparer.OrdinalIgnoreCase);

  private readonly Dictionary<string, T>.AlternateLookup<
      ReadOnlySpan<char>> spanImpl_;

  public CaseInvariantStringDictionary() => this.spanImpl_
      = this.impl_.GetAlternateLookup<ReadOnlySpan<char>>();

  private readonly object lock_ = new();

  public void Clear() {
    lock (this.lock_) {
      this.impl_.Clear();
    }
  }

  public int Count => this.impl_.Count;

  public T GetOrAdd(string key, Func<string, T> createHandler) {
    lock (this.lock_) {
      ref var value = ref CollectionsMarshal.GetValueRefOrAddDefault(
          this.impl_,
          key,
          out var exists);
      if (exists) {
        return value!;
      }

      return value = createHandler(key);
    }
  }

  public T GetOrAdd(ReadOnlySpan<char> key,
                    Func<ReadOnlySpan<char>, T> createHandler) {
    lock (this.lock_) {
      ref var value = ref CollectionsMarshal.GetValueRefOrAddDefault(
          this.spanImpl_,
          key,
          out var exists);
      if (exists) {
        return value!;
      }

      return value = createHandler(key);
    }
  }

  public IEnumerable<string> Keys => this.impl_.Keys;
  public IEnumerable<T> Values => this.impl_.Values;
  public bool ContainsKey(string key) => this.impl_.ContainsKey(key);

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public bool Remove(string key) => this.impl_.Remove(key);

  public T this[string key] {
    get => this.impl_[key];
    set {
      lock (this.lock_) {
        this.impl_[key] = value;
      }
    }
  }

  public T this[ReadOnlySpan<char> key] {
    get => this.spanImpl_[key];
    set {
      lock (this.lock_) {
        this.spanImpl_[key] = value;
      }
    }
  }

  IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

  public IEnumerator<(string Key, T Value)> GetEnumerator() {
    foreach (var (key, value) in this.impl_) {
      yield return (key, value);
    }
  }
}