namespace FellowshipSim.Core.Heroes.Mara.Talents;

/// <summary>Talent 163: "Your Hemorrhaging Strike bleed ticks faster." UNVERIFIED: no combat log has this talent.</summary>
public partial class Bloodrush : Talent
{
    public override string Name => TalentName;
    public override int Id => TalentId;

    public override void Bind(Hero hero) => ((Mara)hero).Hemorrhage.TickRateBonuses.Add(_ => INCREASED_TICK_RATE_WHILE_SINGLE_TICK_RATE_INCREASE - 1.0);
}
