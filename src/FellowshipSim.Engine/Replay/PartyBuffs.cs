using FellowshipSim.Core.Customization.Sigils;

namespace FellowshipSim.Engine.Replay;

/// <summary>
/// Buffs other players put on the replayed player that change the player's damage. In a replay, a logged buff from
/// another player is applied with the effect listed here (one instance per giver; the log decides when it ends). Logged
/// buffs not listed here are reported as unmodeled in the review: add them here when they matter.
/// </summary>
public static class PartyBuffs
{
    /// <summary>
    /// A buff from another player. When it comes from one of the giver's sigils, <see cref="ValueByGiverRank"/> holds its
    /// modifier value at each sigil rank, and the giver's rank (gear ranks + weapon-tree points) picks the value.
    /// </summary>
    public sealed record PartyBuff(AuraDef Aura, IReadOnlyList<double>? ValueByGiverRank = null, string? SigilName = null, int SigilId = 0);

    public const int ImbuedBloodEffectId = 55;
    public const int ShadowsDefeatEffectId = 2221;

    public static readonly PartyBuff[] All =
    [
        // Inspired Allegiance (sigil 2): Haste rating for the giver and allies, by the giver's rank.
        new(AuraDef.Effect(InspiredAllegiance.SigilName, InspiredAllegiance.BuffEffectId,
                modifiers: StatModifier.Add(StatType.HasteRating, InspiredAllegiance.HasteRatingByRank[0])),
            InspiredAllegiance.HasteRatingByRank, InspiredAllegiance.SigilName, InspiredAllegiance.SigilId),

        // Imbued Blood: +1% Expertise per stack, up to 15 stacks.
        new(AuraDef.Effect("Imbued Blood", ImbuedBloodEffectId, maxStacks: 15, modifiers: StatModifier.Add(StatType.Expertise, 0.01))),

        // Shadow's Defeat: 20% more damage and 20% increased Haste for 60 seconds.
        new(AuraDef.Effect("Shadow's Defeat", ShadowsDefeatEffectId, 60,
            modifiers: [StatModifier.More(StatType.DamageScale, 1.2), StatModifier.Increased(StatType.Haste, 1.2)])),
    ];

    public static PartyBuff? Find(int logId) => All.FirstOrDefault(b => b.Aura.LogId == logId);
}
