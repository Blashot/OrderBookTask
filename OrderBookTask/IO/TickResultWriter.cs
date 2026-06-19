using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace OrderBookTask.IO;

public static class TickResultWriter
{
    public static void Write(string path, ReadOnlySpan<Tick> ticks, ReadOnlySpan<TickResult> results)
    {
        using var writer = new StreamWriter(path, append: false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), bufferSize: 1 << 20);
        writer.WriteLine("SourceTime;Side;Action;OrderId;Price;Qty;B0;BQ0;BN0;A0;AQ0;AN0");

        for (var i = 0; i < ticks.Length; i++)
        {
            WriteLine(writer, ticks[i], results[i]);
        }
    }

    private static void WriteLine(StreamWriter writer, Tick tick, TickResult result)
    {
        writer.Write(tick.SourceTime.ToString(CultureInfo.InvariantCulture));
        writer.Write(';');
        WriteByteAsCharOrEmpty(writer, tick.Side);
        writer.Write(';');
        writer.Write((char)tick.Action);
        writer.Write(';');
        writer.Write(tick.OrderId.ToString(CultureInfo.InvariantCulture));
        writer.Write(';');
        writer.Write(tick.Price.ToString(CultureInfo.InvariantCulture));
        writer.Write(';');
        writer.Write(tick.Qty.ToString(CultureInfo.InvariantCulture));
        writer.Write(';');
        WriteIntOrEmpty(writer, result.B0);
        writer.Write(';');
        WriteLongOrEmpty(writer, result.BQ0);
        writer.Write(';');
        WriteIntOrEmpty(writer, result.BN0);
        writer.Write(';');
        WriteIntOrEmpty(writer, result.A0);
        writer.Write(';');
        WriteLongOrEmpty(writer, result.AQ0);
        writer.Write(';');
        WriteIntOrEmpty(writer, result.AN0);
        writer.WriteLine();
    }

    private static void WriteByteAsCharOrEmpty(TextWriter writer, byte value)
    {
        if (value != 0)
        {
            writer.Write((char)value);
        }
    }

    private static void WriteIntOrEmpty(TextWriter writer, int value)
    {
        if (value != 0)
        {
            writer.Write(value.ToString(CultureInfo.InvariantCulture));
        }
    }

    private static void WriteLongOrEmpty(TextWriter writer, long value)
    {
        if (value != 0)
        {
            writer.Write(value.ToString(CultureInfo.InvariantCulture));
        }
    }
}
