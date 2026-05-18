using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace fin.data.dictionaries;

public sealed class SimpleDictionary<TKey, TValue>(
    Dictionary<TKey, TValue> impl)
    : IFinDictionary<TKey, TValue> where TKey : notnull {
  private readonly object lock_ = new();

  public SimpleDictionary() : this(new Dictionary<TKey, TValue>()) { }

  public SimpleDictionary(IEqualityComparer<TKey> comparer) : this(
      new Dictionary<TKey, TValue>(comparer)) { }

  public void Clear() {
    lock (this.lock_) {
      impl.Clear();
    }
  }

  public int Count => impl.Count;
  public IEnumerable<TKey> Keys => impl.Keys;
  public IEnumerable<TValue> Values => impl.Values;
  public bool ContainsKey(TKey key) => impl.ContainsKey(key);

  public TValue GetOrAdd(TKey key, Func<TKey, TValue> createHandler) {
    lock (this.lock_) {
      ref var value = ref CollectionsMarshal.GetValueRefOrAddDefault(
          impl,
          key,
          out var exists);
      if (exists) {
        return value!;
      }

      return value = createHandler(key);
    }
  }

  public TValue this[TKey key] {
    get => impl[key];
    set {
      lock (this.lock_) {
        impl[key] = value;
      }
    }
  }

  public bool Remove(TKey key) {
    lock (this.lock_) {
      return impl.Remove(key);
    }
  }

  IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

  public IEnumerator<(TKey Key, TValue Value)> GetEnumerator() {
    foreach (var (key, value) in impl) {
      yield return (key, value);
    }
  }
}