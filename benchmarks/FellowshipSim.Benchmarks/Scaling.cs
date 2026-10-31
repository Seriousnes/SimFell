using System.Diagnostics;
using System.Runtime.InteropServices;
using FellowshipSim.Core.Engine;
using FellowshipSim.Engine.Replay;

namespace FellowshipSim.Benchmarks;

/// <summary>
/// Many iterations through the public entry points (<see cref="MonteCarloSimulator.Run"/>, <see cref="FreeDiceReplay.Run"/>)
/// at fixed core counts. Each core count runs in its own process: the process is pinned to those logical processors,
/// and the runtime's processor count and server GC heap count equal the core count. Code that sizes its parallelism from
/// <see cref="Environment.ProcessorCount"/> therefore runs at that degree of parallelism.
/// </summary>
public static class Scaling
{
    public const string RunMode = "scale-run";
    public const string DefaultIterationsText = "100000";
    private const string ProcessorCountVariable = "DOTNET_PROCESSOR_COUNT";
    private const string GcHeapCountVariable = "DOTNET_GCHeapCount";
    private const string GcHeapAffinitizeMaskVariable = "DOTNET_GCHeapAffinitizeMask";
    private const string HexFormat = "x";

    /// <summary>Core counts run when none are given.</summary>
    private static readonly int[] DefaultCoreCounts = [1, 2, 4, 8];

    /// <summary>Milliseconds between checks that a child process has exited.</summary>
    private const int ExitPollMilliseconds = 1000;

    private const double BytesPerGigabyte = 1e9;

    /// <summary>Most processors a range is written out in full for.</summary>
    private const int MaxListedProcessors = 2;

    // SYSTEM_CPU_SET_INFORMATION byte offsets.
    private const int LogicalProcessorIndexOffset = 14;
    private const int CoreIndexOffset = 15;
    private const int EfficiencyClassOffset = 18;

    /// <summary>
    /// <c>scale &lt;mc|mc8|mc-idle|mc8-idle|freedice&gt; [iterations] [core counts]</c>: one process per core count (default 1, 2, 4 and 8),
    /// then a table.
    /// </summary>
    public static void Run(string[] args)
    {
        var workload = args.ElementAtOrDefault(1) ?? Fight.DefaultWorkload;
        var iterations = int.Parse(args.ElementAtOrDefault(2) ?? DefaultIterationsText);
        var order = ProcessorOrder();
        var counts = args.ElementAtOrDefault(3) is { } list
            ? list.Split(',').Select(int.Parse).ToList()
            : DefaultCoreCounts.Where(n => n <= order.Count).ToList();

        Console.WriteLine($"{workload}, {iterations:N0} iterations. Logical processors, first chosen first: {string.Join(",", order)}");
        Console.WriteLine("cores  logical processors        wall s    CPU s   iter/s  efficiency  allocated GB   gen0  gen1  gen2  GC pause  peak GB");
        double? single = null;
        foreach (var cores in counts)
        {
            var chosen = order.Take(cores).ToList();
            var mask = chosen.Aggregate(0L, (m, p) => m | (1L << p));
            var start = new ProcessStartInfo(Environment.ProcessPath!)
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
            };
            foreach (var argument in new[] { RunMode, workload, iterations.ToString(), mask.ToString() }) start.ArgumentList.Add(argument);
            start.Environment[ProcessorCountVariable] = cores.ToString();
            start.Environment[GcHeapCountVariable] = cores.ToString(HexFormat);
            start.Environment[GcHeapAffinitizeMaskVariable] = mask.ToString(HexFormat);
            using var child = Process.Start(start)!;
            var output = child.StandardOutput.ReadToEndAsync();
            var clock = Stopwatch.StartNew();
            var peak = 0L;
            while (!child.WaitForExit(ExitPollMilliseconds))
            {
                child.Refresh();
                // The child can exit between the wait and the read.
                try { peak = Math.Max(peak, child.WorkingSet64); }
                catch (InvalidOperationException) { break; }
                if (peak <= WorkingSetLimitBytes) continue;
                child.Kill(entireProcessTree: true);
                child.WaitForExit();
                Console.WriteLine($"{cores,5}  {Ranges(chosen),-22} aborted: working set passed {WorkingSetLimitBytes / BytesPerGigabyte:F0} GB after {clock.Elapsed.TotalSeconds:F0} s");
                break;
            }
            if (peak > WorkingSetLimitBytes) continue;
            var measurement = Measurement.Parse(output.Result.Trim().Split('\n').Last().Trim());
            var rate = iterations / measurement.WallSeconds;
            single ??= cores == 1 ? rate : null;
            var efficiency = single is { } one ? $"{rate / (one * cores),9:P0}" : "        -";
            Console.WriteLine($"{cores,5}  {Ranges(chosen),-22} {measurement.WallSeconds,9:F1} {measurement.CpuSeconds,8:F1} {rate,8:N0}  {efficiency}  {measurement.AllocatedBytes / BytesPerGigabyte,12:F1} {measurement.Gen0,6} {measurement.Gen1,5} {measurement.Gen2,5} {measurement.PausePercent,8:P1} {measurement.PeakWorkingSetBytes / BytesPerGigabyte,8:F2}");
        }
    }

    /// <summary>A run whose working set passes this is stopped (a fight that leaks state into the next grows without bound).</summary>
    private const long WorkingSetLimitBytes = 8_000_000_000;

    /// <summary>The child process: pins itself, runs the workload once and prints one line of measurements.</summary>
    public static void RunChild(string[] args)
    {
        var workload = args[1];
        var iterations = int.Parse(args[2]);
        if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux())
        {
            Process.GetCurrentProcess().ProcessorAffinity = (nint)long.Parse(args[3]);
        }

        var cores = System.Numerics.BitOperations.PopCount(ulong.Parse(args[3]));
        var work = Fight.MonteCarloWorkload(workload) is { Targets: var targets, Idle: var idle }
            ? MonteCarlo(iterations, targets, idle, cores)
            : workload == Fight.FreeDiceWorkload
                ? FreeDice(iterations)
                : throw new ArgumentException($"Unknown workload '{workload}' (mc, mc8, mc-idle, mc8-idle or freedice)");

        var process = Process.GetCurrentProcess();
        var cpu = process.TotalProcessorTime;
        var allocated = GC.GetTotalAllocatedBytes(precise: true);
        var pause = GC.GetTotalPauseDuration();
        var gen0 = GC.CollectionCount(generation: 0);
        var gen1 = GC.CollectionCount(generation: 1);
        var gen2 = GC.CollectionCount(generation: 2);
        var clock = Stopwatch.StartNew();
        work();
        clock.Stop();
        process.Refresh();
        var measurement = new Measurement(
            clock.Elapsed.TotalSeconds,
            (process.TotalProcessorTime - cpu).TotalSeconds,
            GC.GetTotalAllocatedBytes(precise: true) - allocated,
            GC.CollectionCount(generation: 0) - gen0, GC.CollectionCount(generation: 1) - gen1, GC.CollectionCount(generation: 2) - gen2,
            (GC.GetTotalPauseDuration() - pause).TotalSeconds / clock.Elapsed.TotalSeconds,
            process.PeakWorkingSet64);
        Console.WriteLine(measurement.ToString());
    }

    /// <summary>One Monte Carlo run of <paramref name="iterations"/> fights on <paramref name="cores"/> threads.</summary>
    private static Action MonteCarlo(int iterations, int targets, bool idle, int cores)
    {
        var simulator = new MonteCarloSimulator(Fight.NewHero, Fight.Actions(idle));
        return () => simulator.Run(Fight.Duration, iterations, targets, degreeOfParallelism: cores);
    }

    private static Action FreeDice(int iterations)
    {
        var script = ReplayScript.Load(FreeDiceBenchmarks.ScriptPath
            ?? throw new InvalidOperationException($"{FreeDiceBenchmarks.ScriptVariable} names no replay script"));
        return () => FreeDiceReplay.Run(script, iterations);
    }

    private sealed record Measurement(double WallSeconds, double CpuSeconds, long AllocatedBytes, int Gen0, int Gen1, int Gen2, double PausePercent, long PeakWorkingSetBytes)
    {
        public override string ToString() =>
            FormattableString.Invariant($"{WallSeconds:R} {CpuSeconds:R} {AllocatedBytes} {Gen0} {Gen1} {Gen2} {PausePercent:R} {PeakWorkingSetBytes}");

        public static Measurement Parse(string line)
        {
            var fields = line.Split(' ');
            var culture = System.Globalization.CultureInfo.InvariantCulture;
            return new(double.Parse(fields[0], culture), double.Parse(fields[1], culture), long.Parse(fields[2], culture), int.Parse(fields[3], culture), int.Parse(fields[4], culture), int.Parse(fields[5], culture), double.Parse(fields[6], culture), long.Parse(fields[7], culture));
        }
    }

    /// <summary>
    /// Logical processors in the order core counts take them: one logical processor of each performance core, then one of
    /// each efficiency core, then the second logical processor of each core (Windows CPU sets; other systems: 0, 1, 2...).
    /// </summary>
    private static List<int> ProcessorOrder()
    {
        if (!OperatingSystem.IsWindows()) return Enumerable.Range(0, Environment.ProcessorCount).ToList();
        GetSystemCpuSetInformation(IntPtr.Zero, 0, out var length, IntPtr.Zero, 0);
        var buffer = Marshal.AllocHGlobal(length);
        try
        {
            if (!GetSystemCpuSetInformation(buffer, length, out length, IntPtr.Zero, 0)) throw new InvalidOperationException("GetSystemCpuSetInformation failed");
            var sets = new List<CpuSet>();
            for (var offset = 0; offset < length;)
            {
                var size = Marshal.ReadInt32(buffer, offset);
                // SYSTEM_CPU_SET_INFORMATION: LogicalProcessorIndex at 14, CoreIndex at 15, EfficiencyClass at 18.
                sets.Add(new CpuSet(Marshal.ReadByte(buffer, offset + LogicalProcessorIndexOffset), Marshal.ReadByte(buffer, offset + CoreIndexOffset), Marshal.ReadByte(buffer, offset + EfficiencyClassOffset)));
                offset += size;
            }
            return sets
                .GroupBy(s => s.Core)
                .SelectMany(core => core.OrderBy(s => s.Logical).Select((s, i) => new ThreadedCpuSet(s.Logical, i, s.Efficiency, s.Core)))
                .OrderBy(s => s.Thread).ThenByDescending(s => s.Efficiency).ThenBy(s => s.Core)
                .Select(s => s.Logical)
                .ToList();
        }
        finally { Marshal.FreeHGlobal(buffer); }
    }

    /// <summary>"0,2,4,6" as "0-6 step 2"; every logical processor as "all"; any other set as its list.</summary>
    private static string Ranges(List<int> processors)
    {
        if (processors.Count == Environment.ProcessorCount && processors.Count > MaxListedProcessors) return $"all {processors.Count}";
        if (processors.Count <= MaxListedProcessors) return string.Join(",", processors);
        var step = processors[1] - processors[0];
        var even = step > 0 && processors.Zip(processors.Skip(1), (first, second) => second - first == step).All(gap => gap);
        return even ? $"{processors[0]}-{processors[^1]}" + (step > 1 ? $" step {step}" : "") : string.Join(",", processors);
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetSystemCpuSetInformation(IntPtr information, int bufferLength, out int returnedLength, IntPtr process, uint flags);

    private readonly record struct CpuSet(int Logical, int Core, int Efficiency);

    private readonly record struct ThreadedCpuSet(int Logical, int Thread, int Efficiency, int Core);
}
