namespace FellowshipSim.Tests.GameDataBefore;

/// <summary>
/// One of today's tables against the generated entity constants and table with its rows: every row of today's table is
/// the entity constant or the table entry of its key, every generated key is a key of today's table or a listed addition,
/// and no key is both an entity constant and a table entry.
/// </summary>
public static class GameTableComparison
{
    /// <summary>The rows compared, the rows differing, and the rows the generated data adds.</summary>
    public sealed record Result(string Table, int Rows, int FromEntities, int FromTable, IReadOnlyList<string> Differences, IReadOnlyList<string> Additions)
    {
        public override string ToString() =>
            $"{Table}: {Rows} rows ({FromEntities} entity constants, {FromTable} table entries), {Differences.Count} differences, {Additions.Count} additions"
            + (Additions.Count > 0 ? $" [{string.Join("; ", Additions)}]" : "")
            + (Differences.Count > 0 ? $" DIFFERENCES: {string.Join("; ", Differences.Take(20))}" : "");
    }

    public static Result Compare(string table, IEnumerable<(string Key, string Value)> before, IEnumerable<(string Key, string Value)> entities,
        IEnumerable<(string Key, string Value)> generated, bool ignoreCase = false)
    {
        var comparer = ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        var old = before.ToDictionary(r => r.Key, r => r.Value, comparer);
        var byEntity = entities.GroupBy(e => e.Key, comparer).ToDictionary(g => g.Key, g => g.Select(e => e.Value).Distinct().ToList(), comparer);
        var byTable = generated.ToDictionary(r => r.Key, r => r.Value, comparer);
        var differences = new List<string>();
        int fromEntities = 0, fromTable = 0;
        foreach (var (key, value) in old)
        {
            bool entity = byEntity.TryGetValue(key, out var values);
            bool listed = byTable.TryGetValue(key, out string? row);
            if (entity && listed) differences.Add($"{key}: an entity constant and a table entry");
            else if (entity && (values!.Count != 1 || values[0] != value)) differences.Add($"{key}: {value} before, entity {string.Join(" | ", values)}");
            else if (listed && row != value) differences.Add($"{key}: {value} before, table {row}");
            else if (!entity && !listed) differences.Add($"{key}: {value} before, none generated");
            if (entity) fromEntities++;
            else if (listed) fromTable++;
        }
        var additions = byEntity.Keys.Concat(byTable.Keys).Where(k => !old.ContainsKey(k)).Distinct(comparer)
            .Select(k => $"{k} = {(byTable.TryGetValue(k, out string? v) ? v : string.Join(" | ", byEntity[k]))}")
            .Order(StringComparer.Ordinal).ToList();
        return new Result(table, old.Count, fromEntities, fromTable, differences, additions);
    }
}
