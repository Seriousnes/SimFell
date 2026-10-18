using FellowshipSim.Core.Heroes.Rime.Abilities;

namespace FellowshipSim.Core.Heroes.Rime.Talents;

/// <summary>Talent 658: Glacial Blast becomes Talon Strike and Ice Comet becomes Rising Talons.</summary>
public class IcyTalons : Talent
{
    public const int TalentId = 658;
    public const string TalentName = "Icy Talons";

    public override string Name => TalentName;
    public override int Id => TalentId;

    public override void Bind(Hero hero) => ((Rime)hero).HasIcyTalons = true;
}
