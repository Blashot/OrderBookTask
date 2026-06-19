namespace OrderBookTask;

public readonly record struct BookLevel(long QtySum, int OrderCount)
{
    public BookLevel Add(int qty) => new(QtySum + qty, OrderCount + 1);
    public BookLevel Remove(int qty) => new(QtySum - qty, OrderCount - 1);
}
