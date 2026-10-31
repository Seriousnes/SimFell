using FellowshipSim.Core.Heroes.Rime.Talents;

namespace FellowshipSim.Core.Heroes.Rime.Abilities;

/// <summary>Ability 1026 / effects 1389 + 2299: +20% Spirit for 20s. Usable during the global cooldown.</summary>
public class WintersBlessing : RimeAbility
{
    public const int AbilityId = 1026;
    public const string AbilityName = "Winter's Blessing";
    public const double CooldownSeconds = 60.0;
    public const int BuffEffectId = 1389;
    public const double BuffSeconds = 20.0;
    /// <summary>+20% Spirit (game data effect 2299; logs only show effect 1389).</summary>
    public const double SpiritBonus = 0.2;

    public static readonly AuraDef Buff = AuraDef.Effect(AbilityName, BuffEffectId, BuffSeconds,
        modifiers: StatModifier.Add(StatType.Spirit, SpiritBonus));

    public WintersBlessing(Rime owner) : base(owner, AbilityId, AbilityName, AbilityCategory.Major)
    {
        SetCooldown(CooldownSeconds);
        IgnoresGcd = true;
    }

    public override void Execute(SimulationContext ctx, EnemyTarget target)
    {
        Rime.Auras.Apply(Buff, ctx.CurrentTime);
        TriggerCooldown(ctx);
    }
}
