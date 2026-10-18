using FellowshipSim.Core.Codex;
using FellowshipSim.Core.Domain;
using FellowshipSim.Core.Engine;
using FellowshipSim.Core.Heroes.Rime.Spells;

namespace FellowshipSim.Core.Heroes.Rime.Talents;

// Talent IDs and values are from the datamine talent records (talent-slot hero "Rime").

/// <summary>Talent 38. Cold Snap grants Glacial Assault stacks; at 4 the next Glacial Blast/Talon Strike is empowered.</summary>
public class GlacialAssault : Talent
{
    public override string Name => "Glacial Assault";
    public override int Id => 38;
    public override void Bind(Hero hero) { }
}

/// <summary>Talent 51 / effect 1372: "The power of your critical strikes is increased by 10%." (CritMultiplier x1.1)</summary>
public class BitingCold : Talent
{
    public const int EffectId = 1372;
    public override string Name => "Biting Cold";
    public override int Id => 51;
    public override void Bind(Hero hero) => hero.PassiveAuras.Add(AuraDef.FromEffect(hero.Data, EffectId));
}

/// <summary>Talent 189: Flight of the Navir grants 2 stacks of Navir's Keeper (free Cold Snaps).</summary>
public class NavirsKeeper : Talent
{
    public const int StacksGranted = 2;
    public override string Name => "Navir's Keeper";
    public override int Id => 189;
    public override void Bind(Hero hero) { }
}

/// <summary>Talent 192: each Anima generated during Ice Blitz conjures 1 Frost Swallow.</summary>
public class CascadingBlitz : Talent
{
    public override string Name => "Cascading Blitz";
    public override int Id => 192;
    public override void Bind(Hero hero) { }
}

/// <summary>
/// Talent 193: Frost Bolt damage triggers a single Bursting Ice pulse, and Bursting Ice deals more damage.
/// The description says 20% more; combat logs match the datamined constant on Bursting Ice
/// ("Talent.IncreasedCastedDebuffAoeDamageDamage.DamageScaler" = 1.3).
/// </summary>
public class Burstbolter : Talent
{
    public const string DamageScalerConstant = "Talent.IncreasedCastedDebuffAoeDamageDamage.DamageScaler";
    public override string Name => "Burstbolter";
    public override int Id => 193;

    public override void Bind(Hero hero)
    {
        double scaler = PlaceholderConstant(hero, BurstingIce.AbilityId, DamageScalerConstant);
        hero.GetAbility<BurstingIce>()?.AddDamageMultiplier(scaler);
    }

    internal static double PlaceholderConstant(Hero hero, int abilityId, string constant)
    {
        foreach (var p in hero.Data.Raw("ability", abilityId).GetProperty("placeholders").EnumerateArray())
        {
            if (p.TryGetProperty("constants", out var constants) && constants.TryGetProperty(constant, out var value))
            {
                return value.GetDouble();
            }
        }
        throw new KeyNotFoundException($"Ability {abilityId} has no placeholder constant {constant}");
    }
}

/// <summary>Talent 628: Frost Swallow hits have a 12% chance to trigger a Bursting Ice pulse on the target.</summary>
public class BurstingSwallows : Talent
{
    public const double ProcChance = 0.12;
    public override string Name => "Bursting Swallows";
    public override int Id => 628;
    public override void Bind(Hero hero) { }
}

/// <summary>Talent 658: Glacial Blast becomes Talon Strike and Ice Comet becomes Rising Talons.</summary>
public class IcyTalons : Talent
{
    public override string Name => "Icy Talons";
    public override int Id => 658;
    public override void Bind(Hero hero) { }
}

/// <summary>Talent 43: Freezing Torrent ticks reduce Bursting Ice's cooldown by 0.2s; Cold Snap reduces Freezing Torrent's by 1.5s.</summary>
public class ChillingFinesse : Talent
{
    public const double BurstingIceReductionSeconds = 0.2;
    public const double FreezingTorrentReductionSeconds = 1.5;
    public override string Name => "Chilling Finesse";
    public override int Id => 43;
    public override void Bind(Hero hero) { }
}

/// <summary>Talent 49: Ice Blitz, Flight of the Navir and Winter's Blessing cooldowns -0.3s per Winter Orb spent or overflowed.</summary>
public class WisdomOfTheNorth : Talent
{
    public const double ReductionPerOrbSeconds = 0.3;
    public override string Name => "Wisdom of the North";
    public override int Id => 49;
    public override void Bind(Hero hero) { }

    public static void ReduceCooldowns(Hero hero, int orbs, SimTime now)
    {
        var amount = SimTime.FromSeconds(ReductionPerOrbSeconds * orbs);
        hero.ReduceCooldown<IceBlitz>(amount, now);
        hero.ReduceCooldown<FlightOfTheNavir>(amount, now);
        hero.ReduceCooldown<WintersBlessing>(amount, now);
    }
}

/// <summary>
/// Talent 190 / effect 1392 ("DamageIncreaseAndCastTimeIncrease"): Glacial Blast deals 40% more damage, cast +0.5s.
/// Talon Strike benefits from talents that affect Glacial Blast (talent 658).
/// </summary>
public class GreaterGlacialBlast : Talent
{
    public const double CastTimeIncreaseSeconds = 0.5;
    public override string Name => "Greater Glacial Blast";
    public override int Id => 190;

    public override void Bind(Hero hero)
    {
        double multiplier = hero.Data.Raw("effect", 1392).GetProperty("talents")
            .GetProperty("DamageIncreaseAndCastTimeIncrease").GetProperty("AdditionalDamage").GetDouble();
        hero.GetAbility<GlacialBlast>()?.AddDamageMultiplier(multiplier);
        hero.GetAbility<TalonStrike>()?.AddDamageMultiplier(multiplier);
    }
}

/// <summary>
/// Talent 627 / effect 2839 (12s, 31 stacks): each Bursting Ice pulse grants a stack; Bursting Ice deals 3% more damage per
/// stack; the stack after 30 consumes them all and takes 5s off Flight of the Navir's cooldown.
/// Values: talent text; hero_data Bursting Ice "StackingDamageBonus" (DamageScalerPerStack 0.03, MaxStacks 31, 5s).
/// Combat logs (g6zBdpvJCmh2HfrD fight 31, 1056 pulses): a pulse deals damage with the stacks held before it, then adds
/// its stack; stacks reach 31 and are removed at once.
/// </summary>
public class HarrowingIce : Talent
{
    public const int EffectId = 2839;
    public const double DamagePerStack = 0.03;
    public const double FlightOfTheNavirReductionSeconds = 5.0;
    public override string Name => "Harrowing Ice";
    public override int Id => 627;
    public override void Bind(Hero hero) { }
}

/// <summary>Talent 527: Freezing Torrent deals 20% more damage and channels 0.8s longer (hero_data DamageScale 1.2, AddedSeconds 0.8).</summary>
public class SupremeTorrent : Talent
{
    public const double DamageMultiplier = 1.2;
    public const double AddedChannelSeconds = 0.8;
    public override string Name => "Supreme Torrent";
    public override int Id => 527;

    public override void Bind(Hero hero)
    {
        var torrent = hero.GetAbility<FreezingTorrent>();
        torrent?.AddDamageMultiplier(DamageMultiplier);
        torrent?.ExtendChannel(AddedChannelSeconds);
    }
}

/// <summary>
/// Talent 36: each Freezing Torrent hit adds a stack of Coalescing Frost (effect 2306) to its target for 3s (refreshed);
/// a crit has a 50% chance to add 2. When it expires, or the enemy dies (per the user), it erupts for 56% Intellect per
/// stack on enemies around it (damage effect 2300). Values: talent text; hero_data Freezing Torrent "DelayedAoe"
/// (DamageDividerToInt 0.56, TargetCountThreshold 5). UNVERIFIED: no combat log has this talent.
/// </summary>
public class CoalescingFrost : Talent
{
    public const int DebuffEffectId = 2306;
    public const int DamageEffectId = 2300;
    public const double DurationSeconds = 3.0;
    public const double CoefficientPerStack = 0.56;
    public const double CritDoubleStackChance = 0.5;
    public const double TargetThreshold = 5;
    public override string Name => "Coalescing Frost";
    public override int Id => 36;
    public override void Bind(Hero hero) => ((Rime)hero).GetAbility<CoalescingFrostEruption>()?.Enable(hero);
}

/// <summary>
/// Talent 657: each Freezing Torrent tick has a 7% chance to cast a free Ice Comet on the target (a plain Ice Comet even
/// with Icy Talons, and it can trigger Avalanche, per the user). hero_data: OnTargetPulsatingAoe.FreeCast.ProcChance 0.07.
/// UNVERIFIED: no combat log has this talent.
/// </summary>
public class ColdShower : Talent
{
    public const double ProcChance = 0.07;
    public const string ProcKey = "cold_shower";
    public override string Name => "Cold Shower";
    public override int Id => 657;
    public override void Bind(Hero hero) { }
}

/// <summary>
/// Talent 48: an Ice Comet cast (also Rising Talons with Icy Talons, and Cold Shower's comet) has an 18% chance to bring 1
/// more Ice Comet, else a 9% chance to bring 2; the extra comets cannot trigger it again (per the user).
/// Values: talent text; hero_data Ice Comet "MultiStrike" (0.18 / 0.09). The roll order and the 0.3s spacing follow the
/// old C++ sim. UNVERIFIED: no combat log has this talent.
/// </summary>
public class Avalanche : Talent
{
    public const double OneExtraChance = 0.18;
    public const double TwoExtraChance = 0.09;
    public const double CometSpacingSeconds = 0.3;
    public const string ProcKey = "avalanche";
    public override string Name => "Avalanche";
    public override int Id => 48;
    public override void Bind(Hero hero) { }
}

/// <summary>
/// Talent 44 / effect 2308 (15s, 2 stacks): Cold Snap grants a stack; the next Glacial Blast is cast with +50% Haste, the
/// next Ice Comet lands 0.5s sooner, and the one consuming it gets +20% crit chance (talent text and hero_data
/// "ReducesCastTimeOnOrbSpender" AddedCritChance 0.2; the buff's own text says +25%). Talon Strike and Rising Talons
/// consume it too (Icy Talons). UNVERIFIED: no combat log has this talent; Ice Comet's impact delay is not modeled.
/// </summary>
public class IcyFlow : Talent
{
    public const int EffectId = 2308;
    public const double CritChance = 0.2;
    public const double CastHaste = 0.5;
    public override string Name => "Icy Flow";
    public override int Id => 44;
    public override void Bind(Hero hero) { }
}

/// <summary>
/// Talent 191 / effect 2305 (12s, 2 stacks): gaining Winter Orbs has a 20% chance to grant a stack; the next Glacial Blast
/// or Ice Comet (Talon Strike / Rising Talons) that deals damage has +100% crit chance (overflow is a grievous crit).
/// hero_data "ChanceOnOrbGain" (ProcChance 0.2, CritChance 1.0, Duration 12). Rolled once per gain that adds orbs, as
/// the old C++ sim did. UNVERIFIED: no combat log has this talent.
/// </summary>
public class FrostweaversWrath : Talent
{
    public const int EffectId = 2305;
    public const double ProcChance = 0.2;
    public const double CritChance = 1.0;
    public const string ProcKey = "frostweavers_wrath";
    public override string Name => "Frostweaver's Wrath";
    public override int Id => 191;
    public override void Bind(Hero hero) { }
}

/// <summary>
/// Talent 195 / effect 1373 (18s): casting an ability can grant Soulfrost Torrent (1.2 PPM, not while it is up); the next
/// Freezing Torrent ticks 40% faster and has +100% crit chance. hero_data Freezing Torrent "BoostByCritProc"
/// (Ppm 1.2, ChannelTickRateIncrease 1.4, CritChance 1.0). UNVERIFIED: no combat log has this talent.
/// </summary>
public class SoulfrostTorrent : Talent
{
    public const int EffectId = 1373;
    public const double ProcsPerMinute = 1.2;
    public const double TickRateMultiplier = 1.4;
    public const double CritChance = 1.0;
    public const string ProcKey = "soulfrost_torrent";
    public override string Name => "Soulfrost Torrent";
    public override int Id => 195;

    public override void Bind(Hero hero)
    {
        var rime = (Rime)hero;
        var rppm = new RealPpm(ProcKey, ProcsPerMinute);
        hero.OnCastCompleted += (_, _, now) =>
        {
            if (hero.SimContext is { } ctx && !rime.Auras.HasAura(rime.Aura.SoulfrostTorrent, now)
                && rppm.Roll(hero, ctx, rime.Aura.SoulfrostTorrent.LogId))
            {
                rime.Auras.Apply(rime.Aura.SoulfrostTorrent, now);
            }
        };
    }
}
