namespace OrderBookTask;

public readonly record struct Order(byte Side, int Price, int Qty);
