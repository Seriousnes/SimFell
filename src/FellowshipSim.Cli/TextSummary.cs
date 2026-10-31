using System.Globalization;
using System.Text;
using FellowshipSim.Engine;

namespace FellowshipSim.Cli;

/// <summary>A sim result as text: mean, lowest and highest fight DPS, standard deviation, fights, then each ability's DPS.</summary>
public static class TextSummary
{
    public static string Format(SimResult result)
    {
        var culture = CultureInfo.InvariantCulture;
        var rows = new List<SummaryRow>
        {
            new SummaryRow("Mean DPS", result.MeanDps.ToString("N1", culture)),
            new SummaryRow("Min DPS", result.MinDps.ToString("N1", culture)),
            new SummaryRow("Max DPS", result.MaxDps.ToString("N1", culture)),
            new SummaryRow("Std dev", result.StandardDeviation.ToString("N1", culture)),
            new SummaryRow("Iterations", result.Iterations.ToString("N0", culture)),
            new SummaryRow("Duration", result.DurationSeconds.ToString("0.###", culture) + " s"),
        };
        var abilities = result.AbilityDps.OrderByDescending(a => a.Value)
            .Select(a => new SummaryRow(a.Key, a.Value.ToString("N1", culture)))
            .ToList();
        var all = rows.Concat(abilities).ToList();
        var labelWidth = all.Max(r => r.Label.Length);
        var valueWidth = all.Max(r => r.Value.Length);

        var builder = new StringBuilder();
        foreach (var (label, value) in rows) builder.AppendLine($"{label.PadRight(labelWidth)}  {value.PadLeft(valueWidth)}");
        if (abilities.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("DPS by ability");
            foreach (var (label, value) in abilities) builder.AppendLine($"{label.PadRight(labelWidth)}  {value.PadLeft(valueWidth)}");
        }
        return builder.ToString();
    }

    private readonly record struct SummaryRow(string Label, string Value);
}
