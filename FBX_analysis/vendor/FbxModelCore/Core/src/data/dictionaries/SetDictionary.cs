using System.Collections;
using System.Collections.Generic;
using System.Linq;

using fin.util.sets;

using readOnly;


namespace fin.data.dictionaries;

[GenerateReadOnly]
public partial interface ISetDictionary<TKey, TValue>
    : IFinCollection<(TKey Key, ISet<TValue> Value)> {
  new ISet<TValue> this[TKey key] { get; }

  [Const]
  new bool TryGetSet(TKey key, out ISet<TValue>? list);

  void Add(TKey key, TValue value);
  void Add(TKey key, IEnumerable<TValue> values);
}

/// <summary>
///   An implementation for a dictionary of sets. Each value added for a key
///   will be stored in that key's corresponding set.
/// </summary>
public sealed class SetDictionary<TKey, TValue>(
    IFinDictionary<TKey, ISet<TValue>> impl)
    : ISetDictionary<TKey, TValue> {
  public SetDictionary() : this(
      new NullFriendlyDictionary<TKey, ISet<TValue>>()) { }

  public void Clear() => impl.Clear();

  public int Count => impl.Values.Select(list => list.Count).Sum();

  public void Add(TKey key, TValue value)
    => impl.GetOrAdd(key, _ => new HashSet<TValue>()).Add(value);

  public void Add(TKey key, IEnumerable<TValue> values)
    => impl.GetOrAdd(key, _ => new HashSet<TValue>()).Add(values);

  public ISet<TValue> this[TKey key] => impl[key];

  public bool TryGetSet(TKey key, out ISet<TValue>? set)
    => impl.TryGetValue(key, out set);

  IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

  public IEnumerator<(TKey Key, ISet<TValue> Value)> GetEnumerator()
    => impl.GetEnumerator();
}