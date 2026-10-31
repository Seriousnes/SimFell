using System.Diagnostics;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using FellowshipSim.Benchmarks;
using FellowshipSim.Core.Engine;
using FellowshipSim.Engine.Replay;

const string CountersMode = "counters";
const string SequenceMode = "sequence";
const string ScaleMode = "scale";
const string ProfileMode = "profile";
const string HeroMode = "hero";
const string DefaultSequenceTargets = "1";
const string DefaultProfileSeconds = "30";
const string JobOption = "--job";
const string JobShortOption = "-j";
const int ProfileBatchFights = 10;
const double MicrosecondsPerMillisecond = 1000;

// counters: the work done in one fight. sequence <targets>: fights one after another on one hero, with each one's time
// and work. profile <mc|mc8|mc-idle|mc8-idle|freedice> <seconds>: fights back to back on one thread, for a trace.
// scale <mc|mc8|mc-idle|mc8-idle|freedice> [iterations] [core counts]: many iterations at fixed core counts. hero: the
// first hero of a new process, then the next thousand. The -idle workloads use the action list that idles.
// Anything else goes to BenchmarkDotNet (e.g. --filter *MonteCarlo*).
switch (args.FirstOrDefault())
{
    case CountersMode:
        Counters.Run();
        return;
    case SequenceMode:
        Counters.FightSequence(int.Parse(args.ElementAtOrDefault(1) ?? DefaultSequenceTargets));
        return;
    case ScaleMode:
        Scaling.Run(args);
        return;
    case Scaling.RunMode:
        Scaling.RunChild(args);
        return;
    case ProfileMode:
        Profile(args.ElementAtOrDefault(1) ?? Fight.DefaultWorkload, double.Parse(args.ElementAtOrDefault(2) ?? DefaultProfileSeconds, System.Globalization.CultureInfo.InvariantCulture));
        return;
    case HeroMode:
        FirstHero();
        return;
}

var types = new List<Type>
{
    typeof(MonteCarloBenchmarks), typeof(IdleMonteCarloBenchmarks), typeof(HeroBenchmarks), typeof(FreeDiceBenchmarks),
    typeof(AplDecisionBenchmarks), typeof(AuraBenchmarks), typeof(HasteAuraBenchmarks), typeof(HitBenchmarks), typeof(EventQueueBenchmarks),
};
if (FreeDiceBenchmarks.ScriptPath == null)
{
    Console.WriteLine($"{FreeDiceBenchmarks.ScriptVariable} names no replay script; the free-dice benchmark is skipped.");
    types.Remove(typeof(FreeDiceBenchmarks));
}

// The same garbage collector as the tests and the server: server GC, not concurrent. A job given on the command line
// (--job dry) replaces it.
var config = args.Any(a => a is JobOption or JobShortOption)
    ? DefaultConfig.Instance
    : DefaultConfig.Instance.AddJob(Job.Default.WithGcServer(true).WithGcConcurrent(false));
BenchmarkSwitcher.FromTypes(types.ToArray()).Run(args, config);

// The first hero includes the catalog's setup and the first compilation of the import.
static void FirstHero()
{
    _ = Fight.Build;
    var clock = Stopwatch.StartNew();
    Fight.NewHero();
    var first = clock.Elapsed.TotalMilliseconds;
    const int heroes = 1000;
    clock.Restart();
    for (var i = 0; i < heroes; i++) Fight.NewHero();
    Console.WriteLine($"first hero {first:F2} ms; next {heroes} heroes {clock.Elapsed.TotalMilliseconds / heroes * MicrosecondsPerMillisecond:F1} µs each");
}

static void Profile(string workload, double seconds)
{
    var clock = Stopwatch.StartNew();
    var fights = 0;
    if (Fight.MonteCarloWorkload(workload) is { Targets: var targets, Idle: var idle })
    {
        var simulator = new MonteCarloSimulator(Fight.NewHero, Fight.Actions(idle));
        while (clock.Elapsed.TotalSeconds < seconds)
        {
            simulator.Run(Fight.Duration, iterations: ProfileBatchFights, targetCount: targets, degreeOfParallelism: 1);
            fights += ProfileBatchFights;
        }
    }
    else if (workload == Fight.FreeDiceWorkload)
    {
        var script = ReplayScript.Load(FreeDiceBenchmarks.ScriptPath
            ?? throw new InvalidOperationException($"{FreeDiceBenchmarks.ScriptVariable} names no replay script"));
        while (clock.Elapsed.TotalSeconds < seconds)
        {
            FreeDiceBenchmarks.Run(script, seed: 1 + fights);
            fights++;
        }
    }
    else
    {
        throw new ArgumentException($"Unknown profile workload '{workload}' (mc, mc8, mc-idle, mc8-idle or freedice)");
    }
    Console.WriteLine($"{workload}: {fights} fights in {clock.Elapsed.TotalSeconds:F1} s ({clock.Elapsed.TotalMilliseconds / fights:F2} ms per fight)");
}
