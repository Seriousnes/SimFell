using System.Collections.Frozen;
using System.Text.Json;
using System.Text.Json.Nodes;
using Fellowship.SDK;
using Fellowship.SDK.Client;
using Fellowship.SDK.Records;

namespace FellowshipSim.Core.Codex;

public class CodexDataService : ICodexDataService
{
    private readonly HttpClient _http;
    private readonly string _cacheDirectory;

    private FrozenDictionary<int, AbilityRecord> _abilities = FrozenDictionary<int, AbilityRecord>.Empty;
    private FrozenDictionary<string, BlessingRecord> _blessings = FrozenDictionary<string, BlessingRecord>.Empty;
    private FrozenDictionary<string, GemTraitRecord> _gemTraits = FrozenDictionary<string, GemTraitRecord>.Empty;
    private FrozenDictionary<string, TraitPowerRecord> _traits = FrozenDictionary<string, TraitPowerRecord>.Empty;
    private FrozenDictionary<string, TalentRecord> _talents = FrozenDictionary<string, TalentRecord>.Empty;
    private FrozenDictionary<int, EntityRecord> _entities = FrozenDictionary<int, EntityRecord>.Empty;

    public CodexClient Client { get; }

    public CodexDataService(HttpClient? http = null, string? cacheDirectory = null)
    {
        _http = http ?? new HttpClient();
        Client = new CodexClient(_http);
        _cacheDirectory = cacheDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".fellowsim", "cache", "codex");
        Directory.CreateDirectory(_cacheDirectory);
    }

    public async Task WarmupCacheAsync(CancellationToken ct = default)
    {
        string entitiesPath = Path.Combine(_cacheDirectory, "entities.jsonl");
        string settingsPath = Path.Combine(_cacheDirectory, "settings.json");

        // Check if cached in ~/.fellowsim/cache/codex/ or fallback to Downloads if available
        if (!File.Exists(entitiesPath))
        {
            string downloadsEntities = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads", "entities.jsonl");
            string downloadsSettings = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads", "settings.json");

            if (File.Exists(downloadsEntities))
            {
                File.Copy(downloadsEntities, entitiesPath, overwrite: true);
                if (File.Exists(downloadsSettings))
                {
                    File.Copy(downloadsSettings, settingsPath, overwrite: true);
                }
            }
            else
            {
                try
                {
                    var client = new CodexClient(_http);
                    byte[] entitiesBytes = await client.ExportEntitiesAsync(ct);
                    await File.WriteAllBytesAsync(entitiesPath, entitiesBytes, ct);

                    byte[] settingsBytes = await client.ExportSettingsAsync(ct);
                    await File.WriteAllBytesAsync(settingsPath, settingsBytes, ct);
                }
                catch
                {
                    // If network is offline, continue with built-in fallbacks
                }
            }
        }

        var abilityDict = new Dictionary<int, AbilityRecord>();
        var blessingDict = new Dictionary<string, BlessingRecord>(StringComparer.OrdinalIgnoreCase);
        var gemTraitDict = new Dictionary<string, GemTraitRecord>(StringComparer.OrdinalIgnoreCase);
        var traitDict = new Dictionary<string, TraitPowerRecord>(StringComparer.OrdinalIgnoreCase);
        var talentDict = new Dictionary<string, TalentRecord>(StringComparer.OrdinalIgnoreCase);
        var entityDict = new Dictionary<int, EntityRecord>();

        if (File.Exists(entitiesPath))
        {
            foreach (var line in await File.ReadAllLinesAsync(entitiesPath, ct))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    var node = JsonNode.Parse(line)?.AsObject();
                    if (node == null) continue;

                    string typeStr = node["type"]?.ToString() ?? node["$type"]?.ToString() ?? string.Empty;
                    if (!node.ContainsKey("type") && !string.IsNullOrEmpty(typeStr))
                    {
                        node["type"] = typeStr;
                    }

                    int id = node["id"]?.GetValue<int>() ?? 0;
                    if (id == 0) continue;

                    string name = node["name"]?.ToString() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(name)) continue;

                    // Clean placeholders with missing 'reading' to avoid deserialization errors
                    if (node["placeholders"] is JsonArray placeholders)
                    {
                        foreach (var item in placeholders)
                        {
                            if (item is JsonObject pObj && !pObj.ContainsKey("reading"))
                            {
                                pObj["reading"] = pObj["key"]?.ToString() ?? pObj["attribute"]?.ToString() ?? "Reading";
                            }
                        }
                    }

                    string normalizedJson = node.ToJsonString();

                    if (typeStr.Equals("ability", StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            var ability = JsonSerializer.Deserialize<AbilityRecord>(normalizedJson, CodexJson.Options);
                            if (ability != null)
                            {
                                abilityDict[ability.Id] = ability;
                                entityDict[ability.Id] = ability;
                            }
                        }
                        catch { }
                    }
                    else if (typeStr.Equals("blessing", StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            var blessing = JsonSerializer.Deserialize<BlessingRecord>(normalizedJson, CodexJson.Options);
                            if (blessing != null)
                            {
                                blessingDict[blessing.Name] = blessing;
                                entityDict[blessing.Id] = blessing;
                            }
                        }
                        catch { }
                    }
                    else if (typeStr.Equals("gem-trait", StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            var gemTrait = JsonSerializer.Deserialize<GemTraitRecord>(normalizedJson, CodexJson.Options);
                            if (gemTrait != null)
                            {
                                string gemColor = node["gem"]?.ToString() ?? gemTrait.GemType.ToString();
                                int nodeNum = node["node"]?.GetValue<int>() ?? gemTrait.Node;

                                if (!string.IsNullOrEmpty(gemColor) && nodeNum > 0)
                                {
                                    gemTraitDict[$"{gemColor}_{nodeNum}"] = gemTrait;
                                }
                                gemTraitDict[$"{gemTrait.GemType}_{gemTrait.Node}"] = gemTrait;
                                if (!string.IsNullOrEmpty(gemTrait.Name)) gemTraitDict[gemTrait.Name] = gemTrait;
                                entityDict[gemTrait.Id] = gemTrait;
                            }
                        }
                        catch { }
                    }
                    else if (typeStr.Equals("trait-power", StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            var trait = JsonSerializer.Deserialize<TraitPowerRecord>(normalizedJson, CodexJson.Options);
                            if (trait != null)
                            {
                                traitDict[trait.Name] = trait;
                                entityDict[trait.Id] = trait;
                            }
                        }
                        catch { }
                    }
                    else if (typeStr.Equals("talent", StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            var talent = JsonSerializer.Deserialize<TalentRecord>(normalizedJson, CodexJson.Options);
                            if (talent != null)
                            {
                                talentDict[talent.Name] = talent;
                                entityDict[talent.Id] = talent;
                            }
                        }
                        catch { }
                    }
                }
                catch
                {
                    // Skip malformed lines
                }
            }
        }

        // Ensure canonical Rime abilities exist
        EnsureCanonicalAbility(abilityDict, 1020, "Cold Snap", 3.145, castTime: 1.5, cooldown: 12.0);
        EnsureCanonicalAbility(abilityDict, 1009, "Glacial Blast", 10.24, castTime: 2.0, cooldown: 0.0);
        EnsureCanonicalAbility(abilityDict, 1028, "Glacial Blast", 10.24, castTime: 2.0, cooldown: 0.0);
        EnsureCanonicalAbility(abilityDict, 2223, "Talon Strike", 4.096, castTime: 0.0, cooldown: 0.0);
        EnsureCanonicalAbility(abilityDict, 2224, "Rising Talons", 2.114, castTime: 0.0, cooldown: 0.0);
        EnsureCanonicalAbility(abilityDict, 1031, "Bursting Ice", 0.498, castTime: 0.0, cooldown: 12.0);
        EnsureCanonicalAbility(abilityDict, 1027, "Freezing Torrent", 1.4806, castTime: 3.0, cooldown: 18.0);
        EnsureCanonicalAbility(abilityDict, 1029, "Frost Bolt", 2.017, castTime: 1.5, cooldown: 0.0);
        EnsureCanonicalAbility(abilityDict, 1023, "Ice Comet", 5.285, castTime: 2.5, cooldown: 30.0);
        EnsureCanonicalAbility(abilityDict, 1018, "Ice Comet", 5.285, castTime: 2.5, cooldown: 30.0);
        EnsureCanonicalAbility(abilityDict, 1001397, "Ice Blitz", 0.0, castTime: 0.0, cooldown: 90.0);
        EnsureCanonicalAbility(abilityDict, 1032, "Ice Blitz", 0.0, castTime: 0.0, cooldown: 90.0);
        EnsureCanonicalAbility(abilityDict, 1001389, "Winter's Blessing", 0.0, castTime: 0.0, cooldown: 60.0);
        EnsureCanonicalAbility(abilityDict, 1026, "Winter's Blessing", 0.0, castTime: 0.0, cooldown: 60.0);
        EnsureCanonicalAbility(abilityDict, 1002446, "Flight of the Navir", 0.0, castTime: 0.0, cooldown: 120.0);
        EnsureCanonicalAbility(abilityDict, 1001387, "Wrath of Winter", 0.0, castTime: 0.0, cooldown: 180.0);
        EnsureCanonicalAbility(abilityDict, 1001, "Shoot", 0.65, castTime: 0.0, cooldown: 2.0);
        EnsureCanonicalAbility(abilityDict, 1558, "Chronoshift", 0.9615, castTime: 3.0, cooldown: 60.0);

        _abilities = abilityDict.ToFrozenDictionary();
        _blessings = blessingDict.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
        _gemTraits = gemTraitDict.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
        _traits = traitDict.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
        _talents = talentDict.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
        _entities = entityDict.ToFrozenDictionary();
    }

    private static void EnsureCanonicalAbility(
        Dictionary<int, AbilityRecord> dict,
        int id,
        string name,
        double coefficient,
        double castTime,
        double cooldown)
    {
        if (dict.TryGetValue(id, out var existing) && existing.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            return;

        var json = $$"""
        {
          "type": "ability",
          "id": {{id}},
          "name": "{{name}}",
          "castTime": {{castTime}},
          "cooldown": {{cooldown}},
          "placeholders": [
            {
              "reading": "{{name}}",
              "attribute": "Intellect",
              "coefficient": {{coefficient}}
            }
          ]
        }
        """;

        try
        {
            var ability = JsonSerializer.Deserialize<AbilityRecord>(json, CodexJson.Options);
            if (ability != null)
            {
                dict[id] = ability;
            }
        }
        catch
        {
            // Ignore if SDK serialization model changes
        }
    }

    public bool TryGetAbilityRecord(int id, out AbilityRecord? record) =>
        _abilities.TryGetValue(id, out record);

    public bool TryGetBlessing(string name, out BlessingRecord? record)
    {
        if (_blessings.TryGetValue(name, out record)) return true;
        if (name.StartsWith("The ", StringComparison.OrdinalIgnoreCase))
            return _blessings.TryGetValue(name[4..], out record);
        return _blessings.TryGetValue($"The {name}", out record);
    }

    public bool TryGetGemTrait(string gemColor, int node, out GemTraitRecord? record)
    {
        if (_gemTraits.TryGetValue($"{gemColor}_{node}", out record)) return true;
        return _gemTraits.TryGetValue($"{gemColor.ToLowerInvariant()}_{node}", out record);
    }

    public bool TryGetGemTrait(string name, out GemTraitRecord? record) =>
        _gemTraits.TryGetValue(name, out record);

    public bool TryGetTraitPower(string name, out TraitPowerRecord? record)
    {
        if (_traits.TryGetValue(name, out record)) return true;
        string normalized = name.Replace(" ", "").Replace("'", "").ToLowerInvariant();
        foreach (var (k, v) in _traits)
        {
            if (k.Replace(" ", "").Replace("'", "").Equals(normalized, StringComparison.OrdinalIgnoreCase))
            {
                record = v;
                return true;
            }
        }
        record = null;
        return false;
    }

    public bool TryGetTalent(string name, out TalentRecord? record) =>
        _talents.TryGetValue(name, out record);

    public async ValueTask<T?> GetAsync<T>(int id) where T : EntityRecord
    {
        if (_entities.TryGetValue(id, out var rec) && rec is T typed)
            return typed;
        try
        {
            return await Client.GetAsync<T>(id);
        }
        catch
        {
            return null;
        }
    }

    public async ValueTask<EntityRecord?> GetAsync(EntityType type, int id)
    {
        if (_entities.TryGetValue(id, out var rec))
            return rec;
        try
        {
            return await Client.GetAsync(type, id);
        }
        catch
        {
            return null;
        }
    }

    public ValueTask<AbilityRecord?> GetAbilityRecordAsync(int id)
    {
        _abilities.TryGetValue(id, out var record);
        return ValueTask.FromResult(record);
    }

    public ValueTask<EntityRecord?> GetEntityRecordAsync(EntityType type, int id)
    {
        _entities.TryGetValue(id, out var record);
        return ValueTask.FromResult(record);
    }
}
