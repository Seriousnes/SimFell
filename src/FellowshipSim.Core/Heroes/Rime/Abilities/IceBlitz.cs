using FellowshipSim.Core.Heroes.Rime.Talents;

namespace FellowshipSim.Core.Heroes.Rime.Abilities;

/// <summary>Ability 1032 / effect 1397: +20% damage for 20s, extended 0.1s per Frost Swallow hit. Usable while casting.</summary>
public class IceBlitz : RimeAbility
{
    public const int AbilityId = 1032;
    public const string AbilityName = "Ice Blitz";
    public const double CooldownSeconds = 60.0;
    public const int BuffEffectId = 1397;
    public const double BuffSeconds = 20.0;
    public const double BuffDamage = 1.2;
    public const double ExtensionPerSwallowSeconds = 0.1;

    public static readonly AuraDef Buff = AuraDef.Effect("Ice Blitz", BuffEffectId, BuffSeconds,
        modifiers: StatModifier.More(StatType.DamageScale, BuffDamage));

    public IceBlitz(Rime owner) : base(owner, AbilityId, AbilityName, AbilityCategory.Major)
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
