using FellowshipSim.Core.Heroes.Rime.Abilities;

namespace FellowshipSim.Core.Heroes.Rime.Talents;

/// <summary>Talent 43: Freezing Torrent ticks reduce Bursting Ice's cooldown by 0.2s; Cold Snap reduces Freezing Torrent's by 1.5s.</summary>
public class ChillingFinesse : Talent
{
    public const int TalentId = 43;
    public const string TalentName = "Chilling Finesse";
    public const double BurstingIceReductionSeconds = 0.2;
    public const double FreezingTorrentReductionSeconds = 1.5;
    public override string Name => TalentName;
    public override int Id => TalentId;
    public override void Bind(Hero hero) { }
}
