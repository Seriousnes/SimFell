using FellowshipSim.Core.Codex;
using FellowshipSim.Core.Domain;
using FellowshipSim.Core.Engine;
using FellowshipSim.Core.Heroes.Rime.Abilities;
using FellowshipSim.Core.Heroes.Rime.Talents;

namespace FellowshipSim.Core.Heroes.Rime;

/// <summary>Rime's auras, defined from their datamine effects (IDs are datamine effect IDs).</summary>
public sealed class RimeAuras
{
    public RimeAuras(Hero hero)
    {
        GlacialAssault = hero.DefineAura(1377);
        NavirsKeeper = hero.DefineAura(2730);
        FlightOfTheNavir = hero.DefineAura(2446);
        WintersBlessing = hero.DefineAura(1389)
            // The Spirit bonus is datamined on effect 2299 (role SpiritBuff); logs only show 1389.
            .WithModifiers(AuraDef.FromEffect(hero.Data, 2299).Modifiers.ToArray());
        WintersEmbrace = hero.DefineAura(2303);
        BurstingIceDebuff = hero.DefineAura(1395);
        IceBlitz = hero.DefineAura(1397);
        WrathOfWinter = hero.DefineAura(1387);
        UndulatingSpirit = hero.DefineAura(2171).WithMaxStacks(2);
        // Combat logs (10 Rime logs): each further Spirit Refund adds 2s to the time left instead of restarting it
        // (removed 10s + 2s per extra stack after the first application, e.g. 3 refunds -> 14.0s).
        RisingSpirit = hero.DefineAura(3115) with { ReapplyExtendsSeconds = 2.0 };
        ChronoshiftBarrier = hero.DefineAura(2279);
        HarrowingIce = hero.DefineAura(Talents.HarrowingIce.EffectId);
        IcyFlow = hero.DefineAura(Talents.IcyFlow.EffectId);
        FrostweaversWrath = hero.DefineAura(Talents.FrostweaversWrath.EffectId);
        SoulfrostTorrent = hero.DefineAura(Talents.SoulfrostTorrent.EffectId);
    }

    public AuraDef GlacialAssault { get; }
    public AuraDef NavirsKeeper { get; }
    public AuraDef FlightOfTheNavir { get; }
    public AuraDef WintersBlessing { get; }
    public AuraDef WintersEmbrace { get; }
    public AuraDef BurstingIceDebuff { get; }
    public AuraDef IceBlitz { get; }
    public AuraDef WrathOfWinter { get; }
    public AuraDef UndulatingSpirit { get; }
    public AuraDef RisingSpirit { get; }
    public AuraDef ChronoshiftBarrier { get; }
    public AuraDef HarrowingIce { get; }
    public AuraDef IcyFlow { get; }
    public AuraDef FrostweaversWrath { get; }
    public AuraDef SoulfrostTorrent { get; }
}
