namespace OrderBookTask;

public readonly record struct Tick(
    long SourceTime,
    byte Side,
    byte Action,
    long OrderId,
    int Price,
    int Qty);
