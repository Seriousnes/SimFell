using System.Globalization;
using FellowshipSim.Core.Engine;
using FellowshipSim.Engine;

namespace FellowshipSim.Cli;

/// <summary>The <c>simfell</c> command line: the profile file, the sim's fights and the output format.</summary>
public sealed record CliOptions(string ProfilePath, SimSettings Settings, CliOutputFormat Format)
{
    private const string OptionPrefix = "--";
    private const string ShortHelpOption = "-h";
    private const string HelpOption = "--help";
    private const string ProfileOption = "--profile";
    private const string IterationsOption = "--iterations";
    private const string DurationOption = "--duration";
    private const string TargetsOption = "--targets";
    private const string SeedOption = "--seed";
    private const string ThreadsOption = "--threads";
    private const string FormatOption = "--format";
    private const string TextFormat = "text";
    private const string JsonFormat = "json";

    /// <summary>
    /// Settings of a command line naming none. Iterations are <see cref="MonteCarloSimulator.Run"/>'s default and the
    /// duration is the benchmarks' Monte Carlo fight; targets, seed and threads are <see cref="SimSettings"/>'s defaults.
    /// </summary>
    public static readonly SimSettings DefaultSettings = new(Iterations: 100, DurationSeconds: 300);

    public static string Usage => $"""
        Usage: simfell --profile <file.simc> [options]

        Runs a Monte Carlo sim of a simc profile. The action list is the profile's actions lines.

        Options:
          --profile <file>      Profile to simulate (required). A relative path is relative to the current directory.
          --iterations <n>      Fights (default {DefaultSettings.Iterations}).
          --duration <seconds>  Length of each fight (default {DefaultSettings.DurationSeconds.ToString(CultureInfo.InvariantCulture)}).
          --targets <n>         Targets in each fight (default {DefaultSettings.TargetCount}).
          --seed <n>            Seed of the first fight; fight i has seed + i (default {DefaultSettings.FirstSeed}).
          --threads <n>         Most threads the fights use (default {MonteCarloSimulator.DefaultDegreeOfParallelism}, 80% of the logical processors).
          --format <text|json>  Result format on stdout (default text). json is the SimResult record.
          -h, --help            This text.

        Progress is on stderr. Ctrl+C cancels the sim (exit code 130).
        """;

    /// <summary>The options in <paramref name="args"/>, or null for <c>--help</c>.</summary>
    /// <exception cref="CliUsageException">An unknown option, a missing or malformed value, or no <c>--profile</c>.</exception>
    public static CliOptions? Parse(IReadOnlyList<string> args)
    {
        string? profile = null;
        var settings = DefaultSettings;
        var format = CliOutputFormat.Text;
        for (var i = 0; i < args.Count; i++)
        {
            var arg = args[i];
            if (arg is ShortHelpOption or HelpOption) return null;
            if (!arg.StartsWith(OptionPrefix, StringComparison.Ordinal)) throw new CliUsageException($"Unexpected argument '{arg}'.");

            var equalsIndex = arg.IndexOf('=');
            var name = equalsIndex < 0 ? arg : arg[..equalsIndex];
            var value = equalsIndex >= 0 ? arg[(equalsIndex + 1)..]
                : i + 1 < args.Count && !args[i + 1].StartsWith(OptionPrefix, StringComparison.Ordinal) ? args[++i]
                : throw new CliUsageException($"{name} needs a value.");
            switch (name)
            {
                case ProfileOption: profile = value; break;
                case IterationsOption: settings = settings with { Iterations = Integer(name, value, min: 1) }; break;
                case DurationOption: settings = settings with { DurationSeconds = Seconds(name, value) }; break;
                case TargetsOption: settings = settings with { TargetCount = Integer(name, value, min: 1) }; break;
                case SeedOption: settings = settings with { FirstSeed = Integer(name, value, min: int.MinValue) }; break;
                case ThreadsOption: settings = settings with { DegreeOfParallelism = Integer(name, value, min: 1) }; break;
                case FormatOption:
                    format = value switch
                    {
                        TextFormat => CliOutputFormat.Text,
                        JsonFormat => CliOutputFormat.Json,
                        _ => throw new CliUsageException($"--format is text or json, not '{value}'."),
                    };
                    break;
                default: throw new CliUsageException($"Unknown option '{name}'.");
            }
        }
        if (string.IsNullOrWhiteSpace(profile)) throw new CliUsageException("--profile is required.");
        return new CliOptions(profile, settings, format);
    }

    private static int Integer(string name, string value, int min) =>
        int.TryParse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var number) && number >= min
            ? number
            : throw new CliUsageException(min == int.MinValue ? $"{name} is a whole number, not '{value}'." : $"{name} is a whole number of at least {min}, not '{value}'.");

    private static double Seconds(string name, string value) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds) && seconds > 0 && double.IsFinite(seconds)
            ? seconds
            : throw new CliUsageException($"{name} is a number of seconds above 0, not '{value}'.");
}
