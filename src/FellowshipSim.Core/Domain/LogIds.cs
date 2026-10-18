namespace FellowshipSim.Core.Domain;

/// <summary>
/// How combat logs number game things: abilities by their game ID, and effects (auras, effect damage), talents, traits and
/// blessings by their game ID plus an offset.
/// </summary>
public static class LogIds
{
    public const int EffectOffset = 1_000_000;
    public const int TalentOffset = 2_000_000;
    public const int TraitOffset = 3_000_000;
    public const int BlessingOffset = 4_000_000;

    /// <summary>The log ID of a game effect.</summary>
    public static int Effect(int effectId) => effectId + EffectOffset;
}
