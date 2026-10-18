using System.Globalization;
using System.Text.RegularExpressions;
using Fellowship.SDK.Records;

namespace FellowshipSim.Core.Codex;

public static class CodexDataMappingExtensions
{
    extension(AbilityRecord record)
    {
        public double PowerCoefficient
        {
            get
            {
                if (record.Placeholders != null)
                {
                    foreach (var p in record.Placeholders)
                    {
                        if (p.Coefficient.HasValue && p.Coefficient.Value > 0)
                            return p.Coefficient.Value;
                    }
                }

                if (record.Coefficients != null)
                {
                    foreach (var c in record.Coefficients)
                    {
                        if (c.Value > 0) return c.Value;
                    }
                }

                return 1.0;
            }
        }

        public double CastTimeSeconds => record.CastTime ?? 0.0;
        public double CooldownSeconds => record.Cooldown ?? 0.0;
    }

    public static double GetPlaceholderValue(this GemTraitRecord? record, string key, double fallback = 0.0)
    {
        if (record?.Placeholders == null) return fallback;
        foreach (var p in record.Placeholders)
        {
            if (string.Equals(p.Reading, key, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(p.Attribute, key, StringComparison.OrdinalIgnoreCase))
            {
                if (p.Value.HasValue)
                {
                    if (string.Equals(p.Format, "delta", StringComparison.OrdinalIgnoreCase) && p.Value.Value > 1.0)
                        return p.Value.Value - 1.0;
                    return p.Value.Value;
                }
                if (p.Coefficient.HasValue) return p.Coefficient.Value;
            }
        }
        return fallback;
    }

    public static List<double> ParseRankValues(string? description, int matchIndex = 0)
    {
        var result = new List<double>();
        if (string.IsNullOrWhiteSpace(description)) return result;

        var matches = Regex.Matches(
            description,
            @"(\+?[\d\.]+%?(?:\s*/\s*\+?[\d\.]+%?)+)");

        if (matchIndex >= 0 && matchIndex < matches.Count)
        {
            var rawGroup = matches[matchIndex].Value;
            var parts = rawGroup.Split('/');
            foreach (var p in parts)
            {
                var trimmed = p.Trim().TrimStart('+');
                bool isPercent = trimmed.EndsWith('%');
                trimmed = trimmed.TrimEnd('%').Trim();
                if (double.TryParse(trimmed, CultureInfo.InvariantCulture, out double val))
                {
                    if (isPercent) val /= 100.0;
                    result.Add(val);
                }
            }
        }

        return result;
    }

    public static double ParsePercentage(string? description, double fallback = 0.0)
    {
        if (string.IsNullOrWhiteSpace(description)) return fallback;
        var match = Regex.Match(description, @"(\d+(?:\.\d+)?)\s*%", RegexOptions.Compiled);
        if (match.Success && double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double val))
        {
            return val / 100.0;
        }
        return fallback;
    }

    public static double GetBonusMultiplier(this TalentRecord? record, double fallback = 1.0)
    {
        if (record == null) return fallback;
        if (record.Placeholders != null)
        {
            foreach (var p in record.Placeholders)
            {
                if (p.Coefficient.HasValue && p.Coefficient.Value > 0)
                    return 1.0 + p.Coefficient.Value;
                if (p.Value.HasValue && p.Value.Value > 0)
                    return 1.0 + (p.Value.Value > 1.0 ? (p.Value.Value - 1.0) : p.Value.Value);
            }
        }
        double pct = ParsePercentage(record.Description, 0.0);
        return pct > 0 ? (1.0 + pct) : fallback;
    }
}
