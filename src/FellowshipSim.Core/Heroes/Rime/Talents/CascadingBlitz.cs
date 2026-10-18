using FellowshipSim.Core.Heroes.Rime.Abilities;

namespace FellowshipSim.Core.Heroes.Rime.Talents;

/// <summary>Talent 192: each Anima generated during Ice Blitz conjures 1 Frost Swallow.</summary>
public class CascadingBlitz : Talent
{
    public const int TalentId = 192;
    public const string TalentName = "Cascading Blitz";
    public override string Name => TalentName;
    public override int Id => TalentId;
    public override void Bind(Hero hero) { }
}
