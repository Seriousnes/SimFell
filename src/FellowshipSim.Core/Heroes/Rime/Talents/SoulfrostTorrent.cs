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
    public const double CritChance = 1.0;
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
