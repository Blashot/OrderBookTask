using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using OrderBookTask;
using OrderBookTask.IO;
using OrderBookTask.Processing;

const string InputFileName = "ticks.raw";
const string OutputFileName = "ticks_result.csv";
const int WarmupRuns = 2;
const int MeasuredRuns = 5;

var inputPath = args.Length > 0 ? args[0] : Path.Combine(AppContext.BaseDirectory, InputFileName);
var outputPath = args.Length > 1 ? args[1] : Path.Combine(AppContext.BaseDirectory, OutputFileName);

if (!File.Exists(inputPath))
{
    Console.Error.WriteLine($"Input file not found: {inputPath}");
    return 1;
}

// Stage 1: Load input data. This stage is not included in performance measurements.
var ticks = TickReader.ReadAll(inputPath);
Console.WriteLine($"Loaded ticks: {ticks.Length:N0}");

var measuredRuns = new RunResult[MeasuredRuns];

// Stage 2: Build the order book. Only this stage is measured.
for (var run = 1; run <= WarmupRuns + MeasuredRuns; run++)
{
    var processor = new OrderBookProcessor();
    var results = new TickResult[ticks.Length];

    var sw = Stopwatch.StartNew();
    processor.Process(ticks, results);
    sw.Stop();

    var usPerTick = sw.Elapsed.TotalMicroseconds / ticks.Length;
    var isWarmup = run <= WarmupRuns;

    if (isWarmup)
    {
        Console.WriteLine($"Warmup {run}: total = {sw.Elapsed.TotalMilliseconds:F3} ms, per tick = {usPerTick:F3} us");
        continue;
    }

    var measuredRun = run - WarmupRuns;
    measuredRuns[measuredRun - 1] = new RunResult(sw.Elapsed, results);

    Console.WriteLine($"   Run {measuredRun}: total = {sw.Elapsed.TotalMilliseconds:F3} ms, per tick = {usPerTick:F3} us");
}

var averageElapsed = TimeSpan.FromTicks((long)measuredRuns.Average(x => x.Elapsed.Ticks));
var bestRun = measuredRuns.MinBy(x => x.Elapsed);

if (bestRun.Results is null)
{
    Console.Error.WriteLine("No measured run was executed.");
    return 1;
}

Console.WriteLine($"Average build time: {averageElapsed.TotalMilliseconds:F3} ms, per tick = {averageElapsed.TotalMicroseconds / ticks.Length:F3} us");
Console.WriteLine($"Best build time: {bestRun.Elapsed.TotalMilliseconds:F3} ms, per tick = {bestRun.Elapsed.TotalMicroseconds / ticks.Length:F3} us");

// Stage 3: Write output file. This stage is not included in performance measurements.
TickResultWriter.Write(outputPath, ticks, bestRun.Results);
Console.WriteLine($"Result saved: {outputPath}");

return 0;

internal readonly record struct RunResult(TimeSpan Elapsed, TickResult[] Results);