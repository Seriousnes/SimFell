namespace FellowshipSim.Core.Heroes.Mara.Abilities;

/// <summary>
/// Ability 1129: Agility damage to the target enemy, with more damage when attacking from behind. Generates Combo Points,
/// more on a Critical Strike, and costs Energy. In Stealth the Stealth Backstab
/// (<see cref="StealthBackstab"/>) replaces it. A sim attacks from behind, so every hit has
/// <see cref="DAMAGE_MULTIPLIER_BEHIND"/>. UNVERIFIED.
/// </summary>
public partial class Backstab : MaraAbility
{
    /// <summary>Backstab hit an enemy (Hemotoxin, Caustic Wounds).</summary>
    public event Action<SimulationContext, EnemyTarget>? Struck;

    public Backstab(Mara owner) : this(owner, AbilityId, AbilityName)
    {
        UnusableInStealth = true;
    }

    /// <summary>For the Stealth Backstab: it is built on Backstab.</summary>
    protected Backstab(Mara owner, int id, string name) : base(owner, id, name, AbilityCategory)
    {
        BaseCoefficient = AGILITY_COEFFICIENT;
        EnergyCost = AbilityCost;
        BreaksStealth = true;
        Hit += (in hit) =>
        {
            if (hit.SimContext is { } ctx) GenerateComboPoints(ctx, hit.IsCrit ? COMBO_POINT_GAIN_AT_CRIT : COMBO_POINT_GAIN, hit.Target);
        };
    }

    public override void Execute(SimulationContext ctx, EnemyTarget target)
    {
        Pay(ctx);
        DealDamage(ctx, target, damageMultiplier: DAMAGE_MULTIPLIER_BEHIND);
        Struck?.Invoke(ctx, target);
    }
}
