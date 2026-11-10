using FellowshipSim.Core.Heroes.Elarion.Abilities;

namespace FellowshipSim.Core.Heroes.Elarion.Talents;

/// <summary>
/// Talent 124: Multishot reduces the cooldown of Starfall Volley. Empowered Multishot reduces the
/// cooldown by more.
/// </summary>
public partial class RepeatingStars : Talent
{
    public override string Name => TalentName;
    public override int Id => TalentId;

    public override void Bind(Hero hero)
    {
        if (hero.GetAbility<Multishot>() is not { } multishot) return;
        multishot.Cast += (ctx, empowered) =>
            hero.ReduceCooldown<StarfallVolley>(SimTime.FromSeconds(empowered ? COOLDOWN_REDUCTION_EMPOWERED_COOLDOWN_REDUCTION_IN_SECONDS : COOLDOWN_REDUCTION_IN_SECONDS), ctx.CurrentTime);
    }
}
