namespace FellowshipSim.Core.Customization.Blessings;

/// <summary>
/// Blessing 702: "Your [POWER] abilities have a 5% / 8% / 13% / 20% chance to generate 1 Spirit Point toward your SPIRIT
/// ability." No effect on damage yet: the engine does not track Spirit Points (replays follow the log's Spirit ability
/// casts).
/// </summary>
public class TheCelestial : Blessing
{
    public const int BlessingId = 702;
    public const string BlessingName = "The Celestial";
    public static readonly double[] ChanceByRank = [0.05, 0.08, 0.13, 0.20];

    public override int Id => BlessingId;
    public override string Name => BlessingName;

    public override void Bind(Hero hero) { }
}
