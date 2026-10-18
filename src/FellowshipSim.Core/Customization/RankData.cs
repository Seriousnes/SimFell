using System.Text.Json;
using FellowshipSim.Core.Codex;

namespace FellowshipSim.Core.Customization;

/// <summary>Per-rank values of traits, blessings and gem nodes from the datamine.</summary>
public static class RankData
{
    /// <summary>A trait's rank values (datamine "trait" record, rankRuns[<paramref name="run"/>].values).</summary>
    public static double TraitValue(GameData data, string traitName, int rank, int run = 0)
    {
        var trait = ByName(data, "trait", traitName);
        var values = trait.GetProperty("rankRuns")[run].GetProperty("values");
        return values[Math.Clamp(rank, 1, values.GetArrayLength()) - 1].GetDouble();
    }

    /// <summary>A placeholder constant of a blessing; per-rank arrays are indexed by <paramref name="rank"/>.</summary>
    public static double BlessingConstant(GameData data, string blessingName, string constant, int rank)
    {
        var blessing = ByName(data, "blessing", blessingName);
        foreach (var p in blessing.GetProperty("placeholders").EnumerateArray())
        {
            if (p.TryGetProperty("constants", out var constants) && constants.TryGetProperty(constant, out var value))
            {
                return value.ValueKind == JsonValueKind.Array
                    ? value[Math.Clamp(rank, 1, value.GetArrayLength()) - 1].GetDouble()
                    : value.GetDouble();
            }
        }
        throw new KeyNotFoundException($"Blessing '{blessingName}' has no constant {constant}");
    }

    /// <summary>Values written as "a / b / c / d" in a description (first <paramref name="series"/>-th such series), for <paramref name="rank"/>.</summary>
    public static double DescriptionRankValue(string description, int rank, int series = 0)
    {
        var values = CodexDataMappingExtensions.ParseRankValues(description, series);
        if (values.Count == 0) throw new InvalidOperationException($"No rank values in: {description}");
        return values[Math.Clamp(rank, 1, values.Count) - 1];
    }

    /// <summary>A placeholder constant of an effect; per-rank arrays are indexed by <paramref name="rank"/>.</summary>
    public static double EffectConstant(GameData data, int effectId, string constant, int rank)
    {
        foreach (var p in data.Raw("effect", effectId).GetProperty("placeholders").EnumerateArray())
        {
            if (p.TryGetProperty("constants", out var constants) && constants.TryGetProperty(constant, out var value))
            {
                return value.ValueKind == JsonValueKind.Array
                    ? value[Math.Clamp(rank, 1, value.GetArrayLength()) - 1].GetDouble()
                    : value.GetDouble();
            }
        }
        throw new KeyNotFoundException($"Effect {effectId} has no constant {constant}");
    }

    /// <summary>The effect a granted entity produces: the effect itself, or the effect applied by a granted ability.</summary>
    public static int? GrantedEffect(GameData data, string grantType, int grantId)
    {
        if (grantType == "effect") return grantId;
        if (grantType != "ability") return null;
        return data.All("effect")
            .Where(e => e.TryGetProperty("appliedBy", out var by) && by.GetProperty("id").GetInt32() == grantId)
            .Select(e => (int?)e.GetProperty("id").GetInt32())
            .FirstOrDefault();
    }

    public static string BlessingDescription(GameData data, string blessingName) =>
        ByName(data, "blessing", blessingName).GetProperty("description").GetString()!;

    private static JsonElement ByName(GameData data, string type, string name) =>
        data.All(type).FirstOrDefault(e => e.TryGetProperty("name", out var n) && n.GetString() == name) is { ValueKind: JsonValueKind.Object } found
            ? found
            : throw new KeyNotFoundException($"Game data has no {type} named '{name}'");

    /// <summary>The unlocked gem trait nodes of <paramref name="color"/> at <paramref name="power"/>, keeping only the highest rank of each pair (node n+5 replaces node n).</summary>
    public static IReadOnlyList<GemNode> UnlockedGemNodes(GameData data, string color, double power)
    {
        var nodes = data.All("gem-trait")
            .Where(g => g.GetProperty("gem").GetString() == color)
            .Select(g => new GemNode(g, data))
            .Where(n => power >= n.RequiredPower)
            .ToList();
        return nodes.Where(n => !nodes.Any(o => o.Node == n.Node + 5)).OrderBy(n => n.Slot).ToList();
    }
}

public sealed class GemNode
{
    private readonly JsonElement _raw;
    private readonly GameData _data;

    public GemNode(JsonElement raw, GameData data)
    {
        _raw = raw;
        _data = data;
        Id = raw.GetProperty("id").GetInt32();
        Node = raw.GetProperty("node").GetInt32();
        RequiredPower = raw.GetProperty("requiredPower").GetDouble();
        Name = raw.GetProperty("name").GetString()!;
    }

    public int Id { get; }
    public int Node { get; }
    public double RequiredPower { get; }
    public string Name { get; }
    /// <summary>The node's slot in its 5-node row (1-5), shared by its rank I and rank II versions.</summary>
    public int Slot => (Node - 1) % 5 + 1;
    public bool IsRankTwo => Node > 5;

    /// <summary>A placeholder value; "delta" values (e.g. 1.15) are returned as the bonus (0.15).</summary>
    public double Value(string key)
    {
        foreach (var p in _raw.GetProperty("placeholders").EnumerateArray())
        {
            if (p.GetProperty("key").GetString() != key) continue;
            double v = p.GetProperty("value").GetDouble();
            return p.TryGetProperty("format", out var f) && f.GetString() == "delta" ? v - 1.0 : v;
        }
        throw new KeyNotFoundException($"Gem node {Name} has no placeholder {key}");
    }

    /// <summary>The effect this node grants (datamine "grants": an effect, or an ability whose applied effect is used).</summary>
    public int? GrantedEffectId =>
        _raw.TryGetProperty("grants", out var grants) && grants.GetArrayLength() > 0
            ? RankData.GrantedEffect(_data, grants[0].GetProperty("type").GetString()!, grants[0].GetProperty("id").GetInt32())
            : null;
}
