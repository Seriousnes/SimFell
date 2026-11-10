using System.Text;
using Fellowship.SDK.Models;

namespace FellowshipSim.Codex.Generators;

/// <summary>
/// The constant names of a record's tuning values. A tuning value's constant is its name in upper case with an underscore
/// between words (<c>MaxCharges</c> is <c>MAX_CHARGES</c>), so it never has the name of a Pascal case member. A name with
/// several values has one constant per value, numbered from 1 in the order of their tuning keys (<c>MAX_STACKS_1</c>,
/// <c>MAX_STACKS_2</c>).
/// </summary>
internal static class TuningNames
{
    /// <summary>
    /// One value of a tuning name: its constant name, its value, its tuning keys (shortest first) and the tuning
    /// reference its summary names the source of.
    /// </summary>
    public sealed record Named<T>(string Name, T Value, string Keys, TuningReference Source);

    /// <summary>A Pascal case name in upper case with an underscore between words: <c>AoeDamageMaxRange</c> is <c>AOE_DAMAGE_MAX_RANGE</c>.</summary>
    public static string ConstantName(string pascal)
    {
        var result = new StringBuilder();
        for (int i = 0; i < pascal.Length; i++)
        {
            char c = pascal[i];
            if (c == '_')
            {
                if (result.Length > 0 && result[result.Length - 1] != '_') result.Append('_');
                continue;
            }
            if (i > 0 && char.IsUpper(c) && result.Length > 0 && result[result.Length - 1] != '_')
            {
                char before = pascal[i - 1];
                bool acronymEnd = char.IsUpper(before) && i + 1 < pascal.Length && char.IsLower(pascal[i + 1]);
                if (char.IsLower(before) || char.IsDigit(before) || acronymEnd) result.Append('_');
            }
            result.Append(char.ToUpperInvariant(c));
        }
        return result.ToString();
    }

    /// <summary>
    /// The record's tuning values by constant name, in the order of the game data. A tuning key with no value of a
    /// constant type (<paramref name="valueOf"/> is null) has no constant.
    /// </summary>
    public static List<Named<T>> Of<T>(IEnumerable<KeyValuePair<string, TuningReference>>? tuning, Func<object?, T?> valueOf)
        where T : struct
    {
        var named = new List<Named<T>>();
        foreach (var group in (tuning ?? []).Where(t => t.Value.Name is { Length: > 0 }).GroupBy(t => ConstantName(t.Value.Name!)))
        {
            var byValue = group
                .Select(t => (t.Key, Reference: t.Value, Value: valueOf(t.Value.Value)))
                .Where(t => t.Value.HasValue)
                .GroupBy(t => t.Value!.Value)
                .Select(g => (Value: g.Key, Entries: g.OrderBy(e => e.Key.Length).ThenBy(e => e.Key, StringComparer.Ordinal).ToList()))
                .OrderBy(g => g.Entries[0].Key.Length).ThenBy(g => g.Entries[0].Key, StringComparer.Ordinal)
                .ToList();
            for (int i = 0; i < byValue.Count; i++)
            {
                var (value, entries) = byValue[i];
                string name = byValue.Count == 1 ? group.Key : $"{group.Key}_{i + 1}";
                // One value: its keys are every key of the name, and its source is the name's first in the game data.
                var keys = byValue.Count == 1
                    ? group.Select(t => t.Key).OrderBy(k => k.Length).ThenBy(k => k, StringComparer.Ordinal)
                    : entries.Select(e => e.Key);
                var source = byValue.Count == 1 ? group.First().Value : entries[0].Reference;
                named.Add(new Named<T>(name, value, string.Join(", ", keys), source));
            }
        }
        return named;
    }
}
