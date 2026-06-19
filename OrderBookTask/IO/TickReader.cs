using System;
using System.Buffers.Binary;
using System.IO;

namespace OrderBookTask.IO;

public static class TickReader
{
    private const int RecordSize = 8 + 1 + 1 + 8 + 4 + 4;

    public static Tick[] ReadAll(string path)
    {
        var bytes = File.ReadAllBytes(path);

        if (bytes.Length % RecordSize != 0)
        {
            throw new InvalidDataException($"Invalid file length {bytes.Length}. Expected a multiple of {RecordSize} bytes.");
        }

        var ticks = new Tick[bytes.Length / RecordSize];
        var span = bytes.AsSpan();

        for (var i = 0; i < ticks.Length; i++)
        {
            var offset = i * RecordSize;

            ticks[i] = new Tick(
                SourceTime: BinaryPrimitives.ReadInt64BigEndian(span.Slice(offset, 8)),
                Side: span[offset + 8],
                Action: span[offset + 9],
                OrderId: BinaryPrimitives.ReadInt64BigEndian(span.Slice(offset + 10, 8)),
                Price: BinaryPrimitives.ReadInt32BigEndian(span.Slice(offset + 18, 4)),
                Qty: BinaryPrimitives.ReadInt32BigEndian(span.Slice(offset + 22, 4)));
        }

        return ticks;
    }
}
