using FellowshipSim.Core.Heroes.Rime.Abilities;

namespace FellowshipSim.Core.Heroes.Rime.Talents;

/// <summary>
/// Talent 195 / effect 1373 (18s): casting an ability can grant Soulfrost Torrent (1.2 PPM, not while it is up); the next
/// Freezing Torrent ticks 40% faster and has +100% crit chance. hero_data Freezing Torrent "BoostByCritProc"
/// (Ppm 1.2, ChannelTickRateIncrease 1.4, CritChance 1.0). UNVERIFIED: no combat log has this talent.
/// </summary>
public class SoulfrostTorrent : Talent
{
    public const int TalentId = 195;
    public const string TalentName = "Soulfrost Torrent";
    public const int EffectId = 1373;
    public const double BuffSeconds = 18.0;
    public const double ProcsPerMinute = 1.2;
    public const double TickRateMultiplier = 1.4;
    /// <summary>
    /// The tooltip says +100% crit chance, but it counts twice: once to guarantee the crit and again in the grievous
    /// overflow, i.e. +200%. Target dummy test (per the user, 30% crit): Soulfrost ticks 1,996-2,311 vs normal crit ticks
    /// 1,150-1,378, ratio 1.70 against (3 + 0.30) / 2 = 1.65 (the tooltip would give 1.15). PCaMVTFbZzNLrfx3 fight 5:
    /// Soulfrost ticks are 3.57x a normal hit, an overflow of ~1.37 with ~37% crit.
    /// </summary>
    public const double CritChance = 2.0;
    public const string ProcKey = "soulfrost_torrent";

    public static readonly AuraDef Buff = AuraDef.Effect("Soulfrost Torrent", EffectId, BuffSeconds);

    public override string Name => TalentName;
    public override int Id => TalentId;

    public override void Bind(Hero hero)
    {
        var rime = (Rime)hero;
        var rppm = new RealPpm(ProcKey, ProcsPerMinute);
        hero.OnCastCompleted += (_, _, now) =>
        {
            if (hero.SimContext is { } ctx && !rime.Auras.HasAura(Buff, now)
                && rppm.Roll(hero, ctx))
            {
                rime.Auras.Apply(Buff, now);
            }
        };
    }
}
