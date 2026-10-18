using FellowshipSim.Core.Heroes.Rime.Talents;

namespace FellowshipSim.Core.Heroes.Rime.Abilities;

/// <summary>Ability 1009 / effect 2446: 5 Frost Swallows circle Rime for 20s.</summary>
public class FlightOfTheNavir : RimeAbility
{
    public const int AbilityId = 1009;
    public const string AbilityName = "Flight of the Navir";
    /// <summary>Per Frost Swallow hit.</summary>
    public const double Coefficient = 0.469;
    public const double CooldownSeconds = 60.0;
    public const int BuffEffectId = 2446;
    public const double BuffSeconds = 20.0;
    public const int SwallowCount = 5;

    public static readonly AuraDef Buff = AuraDef.Effect("Flight of the Navir", BuffEffectId, BuffSeconds);

    public FlightOfTheNavir(Rime owner) : base(owner, AbilityId, AbilityName, AbilityCategory.Major)
    {
        BaseCoefficient = Coefficient;
        SetCooldown(CooldownSeconds, scalesWithHaste: true);
    }

    public override void Execute(SimulationContext ctx, EnemyTarget target)
    {
        var hero = Rime;
        hero.Auras.Apply(Buff, ctx.CurrentTime);
        if (hero.HasTalent<NavirsKeeper>())
        {
            hero.Auras.Apply(NavirsKeeper.Buff, ctx.CurrentTime, setStacks: NavirsKeeper.StacksGranted);
        }
        TriggerCooldown(ctx);
    }
}
