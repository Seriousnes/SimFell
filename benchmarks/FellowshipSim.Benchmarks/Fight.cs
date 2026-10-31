using System.Text.Json;
using FellowshipSim.Core.APL;
using FellowshipSim.Core.Domain;
using FellowshipSim.Core.Engine;
using FellowshipSim.Core.Import;

namespace FellowshipSim.Benchmarks;

/// <summary>
/// The Rime fight every benchmark uses: the test fixture's character (tests/FellowshipSim.Tests/Fixtures/rime_character.json)
/// with the action list in Fixtures/rime.simc, against training mobs.
/// </summary>
public static class Fight
{
    public const string DefaultWorkload = McWorkload;
    public const string McWorkload = "mc";
    public const string Mc8Workload = "mc8";
    public const string McIdleWorkload = "mc-idle";
    public const string Mc8IdleWorkload = "mc8-idle";
    public const string FreeDiceWorkload = "freedice";

    private const string FrostBoltAction = "frost_bolt";
    private const char CommentMarker = '#';
    private const string FixturesFolder = "Fixtures";
    private const string SolutionFile = "FellowshipSim.slnx";
    private const string FixtureCharacterFile = "rime_character.json";
    private const string FixtureActionListFile = "rime.simc";

    /// <summary>Length of a Monte Carlo fight, in seconds.</summary>
    private const int DurationSeconds = 300;

    /// <summary>Fight time of the hero the micro-benchmarks start from, in seconds.</summary>
    private const int MidFightSeconds = 60;

    /// <summary>Targets of the multi-target workloads.</summary>
    private const int MultiTargets = 8;

    /// <summary>Length of a Monte Carlo fight.</summary>
    public static readonly SimTime Duration = SimTime.FromSeconds(DurationSeconds);

    /// <summary>Fight time of the hero the micro-benchmarks start from.</summary>
    public static readonly SimTime MidFight = SimTime.FromSeconds(MidFightSeconds);

    public static CharacterBuild Build { get; } =
        JsonSerializer.Deserialize<CharacterBuild>(File.ReadAllText(FixturePath(FixtureCharacterFile)))!;

    public static string AplText { get; } = File.ReadAllText(FixturePath(FixtureActionListFile));

    public static AplActionLists Actions() => AplParser.ParseScript(AplText);

    /// <summary>
    /// The fixture's action list without Frost Bolt, its action that is always ready: the hero is free with nothing to cast
    /// whenever every other action waits for a cooldown, a resource or a condition.
    /// </summary>
    public static string IdleAplText { get; } = string.Join('\n', AplText.Split('\n').Where(line =>
        line.Trim() is not { Length: > 0 } action || action.StartsWith(CommentMarker) || AplParser.ParseActionLine(action) is not { AbilityName: FrostBoltAction }));

    public static AplActionLists IdleActions() => AplParser.ParseScript(IdleAplText);

    /// <summary>The fixture's action list, or the one that idles (<see cref="IdleAplText"/>).</summary>
    public static AplActionLists Actions(bool idle) => idle ? IdleActions() : Actions();

    /// <summary>
    /// The Monte Carlo workloads of the command modes: <c>mc</c> (1 target), <c>mc8</c> (8 targets), and <c>mc-idle</c> and
    /// <c>mc8-idle</c> with the action list that idles. Null for another name.
    /// </summary>
    public static MonteCarloWorkloadSetup? MonteCarloWorkload(string workload) => workload switch
    {
        McWorkload => new MonteCarloWorkloadSetup(Targets: 1, Idle: false),
        Mc8Workload => new MonteCarloWorkloadSetup(Targets: MultiTargets, Idle: false),
        McIdleWorkload => new MonteCarloWorkloadSetup(Targets: 1, Idle: true),
        Mc8IdleWorkload => new MonteCarloWorkloadSetup(Targets: MultiTargets, Idle: true),
        _ => null,
    };

    public static Hero NewHero() => HeroImporter.Import(Build);

    /// <summary>A new fight on <paramref name="hero"/> against <paramref name="targets"/> training mobs, set up as <see cref="MonteCarloSimulator"/> sets up each fight.</summary>
    public static SimulationContext Start(Hero hero, int targets)
    {
        var ctx = new SimulationContext(hero);
        hero.ResetForIteration();
        hero.BeginFight(ctx);
        for (int i = 1; i <= targets; i++)
        {
            ctx.AddEnemy(MonteCarloSimulator.TrainingMobIdBase + i, i, $"Training Mob {i}", maxHealth: MonteCarloSimulator.TrainingMobMaxHealth);
        }
        ctx.Player.CurrentTarget = ctx.AllEnemies.First();
        return ctx;
    }

    /// <summary>
    /// Runs the fight to <paramref name="until"/> with <see cref="MonteCarloSimulator"/>'s decision loop. Each pass of the
    /// loop is counted in <paramref name="counts"/> when given.
    /// </summary>
    public static void RunTo(Hero hero, SimulationContext ctx, AplActionLists actions, SimTime until, LoopCounts? counts = null)
    {
        while (ctx.CurrentTime < until)
        {
            bool free = hero.CurrentCast == null && hero.CurrentChannel == null && ctx.CurrentTime >= hero.GlobalCooldownEndTime;
            bool acted = free && actions.TryAct(hero, ctx);
            if (counts != null)
            {
                counts.Passes++;
                if (free) counts.Decisions++;
                if (free && !acted) counts.EmptyDecisions++;
            }

            var next = hero.GlobalCooldownEndTime;
            if (hero.CurrentCast != null && hero.CurrentCast.EndTime > next) next = hero.CurrentCast.EndTime;
            if (hero.CurrentChannel != null && hero.CurrentChannel.Ticks.End > next) next = hero.CurrentChannel.Ticks.End;
            if (!acted && next <= ctx.CurrentTime)
            {
                next = actions.NextDecision(hero, ctx);
                if (counts != null) counts.IdleWakes++;
            }
            ctx.RunUntil(next < until ? next : until);
        }
    }

    /// <summary>A hero <see cref="MidFight"/> into a fight against <paramref name="targets"/> training mobs.</summary>
    public static MidFightState AtMidFight(int targets)
    {
        var hero = NewHero();
        var ctx = Start(hero, targets);
        var actions = Actions();
        RunTo(hero, ctx, actions, MidFight);
        return new MidFightState(hero, ctx, actions);
    }

    /// <summary>
    /// The path of a fixture: next to the program (Fixtures/), else in the repository (the benchmark's Fixtures folder or the
    /// test fixtures). BenchmarkDotNet runs the benchmarks from a build in a subfolder of the program's folder.
    /// </summary>
    private static string FixturePath(string name)
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
        {
            string local = Path.Combine(dir.FullName, FixturesFolder, name);
            if (File.Exists(local)) return local;
            if (File.Exists(Path.Combine(dir.FullName, SolutionFile)))
            {
                foreach (string path in new[]
                         {
                             Path.Combine(dir.FullName, "benchmarks", "FellowshipSim.Benchmarks", FixturesFolder, name),
                             Path.Combine(dir.FullName, "tests", "FellowshipSim.Tests", FixturesFolder, name),
                         })
                {
                    if (File.Exists(path)) return path;
                }
            }
        }
        throw new FileNotFoundException($"Fixture {name} not found from {AppContext.BaseDirectory}");
    }
}

/// <summary>Passes of the decision loop in one fight.</summary>
public sealed class LoopCounts
{
    /// <summary>Passes of the loop.</summary>
    public int Passes;
    /// <summary>Passes where the hero was free to act (one action-list evaluation each).</summary>
    public int Decisions;
    /// <summary>Decisions that cast nothing.</summary>
    public int EmptyDecisions;
    /// <summary>Passes where the hero was free and cast nothing, so the clock went to the next event or change the action list reads.</summary>
    public int IdleWakes;
}
