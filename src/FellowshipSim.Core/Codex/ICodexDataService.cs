using Fellowship.SDK;
using Fellowship.SDK.Client;
using Fellowship.SDK.Records;

namespace FellowshipSim.Core.Codex;

public interface ICodexDataService
{
    CodexClient Client { get; }
    ValueTask<T?> GetAsync<T>(int id) where T : EntityRecord;
    ValueTask<EntityRecord?> GetAsync(EntityType type, int id);
    ValueTask<AbilityRecord?> GetAbilityRecordAsync(int id);
    ValueTask<EntityRecord?> GetEntityRecordAsync(EntityType type, int id);
    bool TryGetAbilityRecord(int id, out AbilityRecord? record);
    bool TryGetBlessing(string name, out BlessingRecord? record);
    bool TryGetGemTrait(string gemColor, int node, out GemTraitRecord? record);
    bool TryGetGemTrait(string name, out GemTraitRecord? record);
    bool TryGetTraitPower(string name, out TraitPowerRecord? record);
    bool TryGetTalent(string name, out TalentRecord? record);
    Task WarmupCacheAsync(CancellationToken ct = default);
}
