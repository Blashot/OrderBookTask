using System;
using System.Collections.Generic;

namespace OrderBookTask.Processing;

public sealed class OrderBookProcessor
{
    private const byte Bid = (byte)'1';
    private const byte Ask = (byte)'2';
    private const byte Add = (byte)'A';
    private const byte Modify = (byte)'M';
    private const byte Delete = (byte)'D';
    private const byte ClearY = (byte)'Y';
    private const byte ClearF = (byte)'F';

    private readonly Dictionary<long, Order> _ordersById = new(capacity: 131_072);

    private readonly Dictionary<int, BookLevel> _bidLevels = new(capacity: 16_384);
    private readonly Dictionary<int, BookLevel> _askLevels = new(capacity: 16_384);

    // SortedSet used as a price index.
    // Best bid/ask cached.
    private readonly SortedSet<int> _bidPrices = new();
    private readonly SortedSet<int> _askPrices = new();

    private int _bestBid;
    private int _bestAsk;

    public void Process(ReadOnlySpan<Tick> ticks, Span<TickResult> results)
    {
        for (var i = 0; i < ticks.Length; i++)
        {
            ProcessTick(ticks[i]);
            results[i] = CreateResult();
        }
    }

    private void ProcessTick(Tick tick)
    {
        switch (tick.Action)
        {
            case ClearY:
            case ClearF:
                Clear();
                break;

            case Add:
            case Modify:
                Upsert(tick);
                break;

            case Delete:
                DeleteOrder(tick.OrderId);
                break;
        }
    }

    private void Upsert(Tick tick)
    {
        if (_ordersById.TryGetValue(tick.OrderId, out var oldOrder))
        {
            RemoveFromLevel(oldOrder);
        }

        var newOrder = new Order(tick.Side, tick.Price, tick.Qty);
        _ordersById[tick.OrderId] = newOrder;
        AddToLevel(newOrder);
    }

    private void DeleteOrder(long orderId)
    {
        if (!_ordersById.Remove(orderId, out var order))
        {
            return;
        }

        RemoveFromLevel(order);
    }

    private void AddToLevel(Order order)
    {
        if (order.Side == Bid)
        {
            AddToBidLevel(order.Price, order.Qty);
        }
        else if (order.Side == Ask)
        {
            AddToAskLevel(order.Price, order.Qty);
        }
    }

    private void AddToBidLevel(int price, int qty)
    {
        if (_bidLevels.TryGetValue(price, out var level))
        {
            _bidLevels[price] = level.Add(qty);
        }
        else
        {
            _bidLevels[price] = new BookLevel(qty, 1);
            _bidPrices.Add(price);
        }

        if (_bestBid == 0 || price > _bestBid)
        {
            _bestBid = price;
        }
    }

    private void AddToAskLevel(int price, int qty)
    {
        if (_askLevels.TryGetValue(price, out var level))
        {
            _askLevels[price] = level.Add(qty);
        }
        else
        {
            _askLevels[price] = new BookLevel(qty, 1);
            _askPrices.Add(price);
        }

        if (_bestAsk == 0 || price < _bestAsk)
        {
            _bestAsk = price;
        }
    }

    private void RemoveFromLevel(Order order)
    {
        switch (order.Side)
        {
            case Bid:
                RemoveFromBidLevel(order.Price, order.Qty);
                break;
            case Ask:
                RemoveFromAskLevel(order.Price, order.Qty);
                break;
        }
    }

    private void RemoveFromBidLevel(int price, int qty)
    {
        var next = _bidLevels[price].Remove(qty);
        if (next.OrderCount > 0)
        {
            _bidLevels[price] = next;
            return;
        }

        _bidLevels.Remove(price);
        _bidPrices.Remove(price);

        if (price == _bestBid)
        {
            _bestBid = _bidPrices.Count == 0 ? 0 : _bidPrices.Max;
        }
    }

    private void RemoveFromAskLevel(int price, int qty)
    {
        var next = _askLevels[price].Remove(qty);
        if (next.OrderCount > 0)
        {
            _askLevels[price] = next;
            return;
        }

        _askLevels.Remove(price);
        _askPrices.Remove(price);

        if (price == _bestAsk)
        {
            _bestAsk = _askPrices.Count == 0 ? 0 : _askPrices.Min;
        }
    }

    private TickResult CreateResult()
    {
        var bidLevel = _bestBid == 0 ? default : _bidLevels[_bestBid];
        var askLevel = _bestAsk == 0 ? default : _askLevels[_bestAsk];

        return new TickResult(
            B0: _bestBid,
            BQ0: bidLevel.QtySum,
            BN0: bidLevel.OrderCount,
            A0: _bestAsk,
            AQ0: askLevel.QtySum,
            AN0: askLevel.OrderCount);
    }

    private void Clear()
    {
        _ordersById.Clear();
        _bidLevels.Clear();
        _askLevels.Clear();
        _bidPrices.Clear();
        _askPrices.Clear();
        _bestBid = 0;
        _bestAsk = 0;
    }
}
