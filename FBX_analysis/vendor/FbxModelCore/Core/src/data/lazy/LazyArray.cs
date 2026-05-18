using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace fin.data.lazy;

/// <summary>
///   Array implementation that lazily populates its entries when accessed.
/// </summary>
public sealed class LazyArray<T> : ILazyArray<T> {
  private readonly T[] impl_;
  private readonly bool[] populated_;
  private readonly Func<int, T> handler_;

  private readonly object lock_ = new();

  public LazyArray(int count, Func<int, T> handler) {
    this.impl_ = new T[count];
    this.populated_ = new bool[count];
    this.handler_ = handler;
  }

  public LazyArray(int count, Func<LazyArray<T>, int, T> handler) {
    this.impl_ = new T[count];
    this.populated_ = new bool[count];
    this.handler_ = (int key) => handler(this, key);
  }

  public int Count => this.impl_.Length;

  public void Clear() {
    lock (this.lock_) {
      for (var i = 0; i < this.Count; ++i) {
        this.populated_[i] = false;
      }
    }
  }

  public T GetOrAdd(int key, Func<int, T> createHandler) {
    lock (this.lock_) {
      if (this.ContainsKey(key)) {
        return this[key];
      }

      return this[key] = createHandler(key);
    }
  }

  public bool ContainsKey(int key)
    => this.populated_.Length > key && this.populated_[key];

  public bool Remove(int key) => this.Remove(key, out _);

  public bool Remove(int key, out T value) {
    lock (this.lock_) {
      if (this.ContainsKey(key)) {
        value = this.impl_[key];
        this.populated_[key] = false;
        return true;
      }

      value = default!;
      return false;
    }
  }

  public T this[int key] {
    get {
      if (this.ContainsKey(key)) {
        return this.impl_[key];
      }

      this.populated_[key] = true;
      return this.impl_[key] = this.handler_(key);
    }
    set {
      lock (this.lock_) {
        this.populated_[key] = true;
        this.impl_[key] = value;
      }
    }
  }

  public IEnumerable<int> Keys
    => Enumerable.Range(0, this.Count).Where(this.ContainsKey);

  public IEnumerable<T> Values
    => this.impl_.Where((value, i) => this.ContainsKey(i));

  IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

  public IEnumerator<(int Key, T Value)> GetEnumerator() {
    for (var i = 0; i < this.populated_.Length; ++i) {
      if (this.populated_[i]) {
        yield return (i, this.impl_[i]);
      }
    }
  }
}