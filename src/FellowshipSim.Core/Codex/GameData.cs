using System.Text.Json;
using System.Text.Json.Nodes;
using Fellowship.SDK.Client;
using Fellowship.SDK.Records;

namespace FellowshipSim.Core.Codex;

/// <summary>
/// The datamined game data (the Codex SDK export, entities.jsonl), indexed by entity type and ID.
/// Each entity is available as its SDK record where the SDK models that type, and always as raw JSON
/// for fields the SDK does not model (e.g. trait rank values).
///
/// Combat logs identify abilities by their ability ID and effects (auras, DoTs, procs) by
/// <see cref="LogIdOffset"/> + effect ID; <see cref="LogId(EffectRecord)"/> gives that ID.
/// </summary>
public sealed class GameData
{
    public const int LogIdOffset = 1_000_000;

    private static readonly Dictionary<string, Type> RecordTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ability"] = typeof(AbilityRecord),
        ["effect"] = typeof(EffectRecord),
        ["talent"] = typeof(TalentRecord),
        ["talent-slot"] = typeof(TalentSlotRecord),
        ["blessing"] = typeof(BlessingRecord),
        ["gem"] = typeof(GemRecord),
        ["gem-trait"] = typeof(GemTraitRecord),
        ["trait-power"] = typeof(TraitPowerRecord),
        ["set-bonus"] = typeof(SetBonusRecord),
        ["set-bonus-power"] = typeof(SetBonusPowerRecord),
        ["legendary-power"] = typeof(LegendaryPowerRecord),
        ["legendary-stat"] = typeof(LegendaryStatRecord),
        ["weapon-power"] = typeof(WeaponPowerRecord),
        ["relic-power"] = typeof(RelicPowerRecord),
        ["modifier"] = typeof(ModifierRecord),
        ["hero"] = typeof(HeroRecord),
        ["creature"] = typeof(CreatureRecord),
        ["item"] = typeof(ItemRecord),
        ["mount"] = typeof(MountRecord),
        ["mount-power"] = typeof(MountPowerRecord),
    };

    private static readonly Lazy<GameData> DefaultInstance = new(() => Load(DefaultPath()));

    private readonly Dictionary<(string Type, int Id), EntityRecord> _records = new();
    private readonly Dictionary<(string Type, int Id), JsonElement> _raw = new();

    private GameData() { }

    /// <summary>The game data from the local Codex cache (~/.fellowsim/cache/codex/entities.jsonl).</summary>
    public static GameData Default => DefaultInstance.Value;

    /// <summary>Entities whose SDK record could not be read; their raw JSON is still available.</summary>
    public IReadOnlyList<string> RecordErrors => _recordErrors;
    private readonly List<string> _recordErrors = new();

    public static string DefaultPath()
    {
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string cached = Path.Combine(home, ".fellowsim", "cache", "codex", "entities.jsonl");
        if (File.Exists(cached)) return cached;
        string downloads = Path.Combine(home, "Downloads", "entities.jsonl");
        if (File.Exists(downloads)) return downloads;
        throw new FileNotFoundException(
            "Game data not found. Export it with the Codex SDK (CodexClient.ExportEntitiesAsync) to ~/.fellowsim/cache/codex/entities.jsonl.", cached);
    }

    public static GameData Load(string path)
    {
        var data = new GameData();
        foreach (var line in File.ReadLines(path))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var node = JsonNode.Parse(line)!.AsObject();
            string type = node["$type"]?.GetValue<string>() ?? node["type"]?.GetValue<string>() ?? string.Empty;
            int id = node["id"]?.GetValue<int>() ?? 0;
            var key = (type, id);

            data._raw[key] = JsonDocument.Parse(line).RootElement.Clone();

            if (!RecordTypes.TryGetValue(type, out var recordType)) continue;
            NormalizeForSdk(node, type);
            try
            {
                if (JsonSerializer.Deserialize(node.ToJsonString(), recordType, CodexJson.Options) is EntityRecord record)
                {
                    data._records[key] = record;
                }
            }
            catch (Exception ex)
            {
                data._recordErrors.Add($"{type} {id}: {ex.Message}");
            }
        }
        return data;
    }

    /// <summary>The export and SDK 1.0.12 disagree on a few required fields; fill them in without changing values.</summary>
    private static void NormalizeForSdk(JsonObject node, string type)
    {
        node["type"] ??= type;
        node["name"] ??= string.Empty;
        foreach (var listName in new[] { "placeholders" })
        {
            if (node[listName] is not JsonArray list) continue;
            foreach (var item in list)
            {
                if (item is JsonObject p && !p.ContainsKey("reading"))
                {
                    p["reading"] = p["key"]?.ToString() ?? p["attribute"]?.ToString() ?? string.Empty;
                }
            }
        }
    }

    // ------------------------------------------------------------------ lookups

    public AbilityRecord Ability(int id) => Get<AbilityRecord>("ability", id);
    public EffectRecord Effect(int id) => Get<EffectRecord>("effect", id);
    public TalentRecord Talent(int id) => Get<TalentRecord>("talent", id);
    public TraitPowerRecord TraitPower(int id) => Get<TraitPowerRecord>("trait-power", id);
    public BlessingRecord Blessing(int id) => Get<BlessingRecord>("blessing", id);
    public GemTraitRecord GemTrait(int id) => Get<GemTraitRecord>("gem-trait", id);
    public SetBonusPowerRecord SetBonusPower(int id) => Get<SetBonusPowerRecord>("set-bonus-power", id);
    public LegendaryPowerRecord LegendaryPower(int id) => Get<LegendaryPowerRecord>("legendary-power", id);
    public WeaponPowerRecord WeaponPower(int id) => Get<WeaponPowerRecord>("weapon-power", id);

    public T Get<T>(string type, int id) where T : EntityRecord =>
        TryGet<T>(type, id, out var record)
            ? record
            : throw new KeyNotFoundException($"Game data has no {type} {id}{(_raw.ContainsKey((type, id)) ? " SDK record (raw JSON exists)" : "")}.");

    public bool TryGet<T>(string type, int id, out T record) where T : EntityRecord
    {
        if (_records.TryGetValue((type, id), out var r) && r is T typed)
        {
            record = typed;
            return true;
        }
        record = null!;
        return false;
    }

    /// <summary>Raw JSON of any entity, including types and fields the SDK does not model.</summary>
    public JsonElement Raw(string type, int id) =>
        _raw.TryGetValue((type, id), out var raw) ? raw : throw new KeyNotFoundException($"Game data has no {type} {id}.");

    public bool TryRaw(string type, int id, out JsonElement raw) => _raw.TryGetValue((type, id), out raw);

    /// <summary>All raw entities of a type.</summary>
    public IEnumerable<JsonElement> All(string type) =>
        _raw.Where(kv => kv.Key.Type.Equals(type, StringComparison.OrdinalIgnoreCase)).Select(kv => kv.Value);

    // ------------------------------------------------------------------ combat log IDs

    public static int LogId(EffectRecord effect) => LogIdOffset + effect.Id;
    public static int LogId(AbilityRecord ability) => ability.Id;
    public static int EffectLogId(int effectId) => LogIdOffset + effectId;
}
