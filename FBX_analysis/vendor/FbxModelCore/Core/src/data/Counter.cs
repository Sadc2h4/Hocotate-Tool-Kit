namespace fin.data;

public sealed class Counter(int startingValue = 0) {
  public int Value { get; set; } = startingValue;

  public int GetAndIncrement() => this.Value++;
}