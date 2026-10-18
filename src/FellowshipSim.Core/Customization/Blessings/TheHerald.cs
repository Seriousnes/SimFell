namespace FellowshipSim.Core.Customization.Blessings;

/// <summary>
/// Blessing 699: "You start dungeons with 12 / 20 / 30 / 50 Spirit Points toward your SPIRIT ability." No effect on damage
/// yet: the engine does not track Spirit Points (replays follow the log's Spirit ability casts).
/// </summary>
public class TheHerald : Blessing
{
    public const int BlessingId = 699;
    public const string BlessingName = "The Herald";
    public static readonly double[] SpiritPointsByRank = [12, 20, 30, 50];

    public override int Id => BlessingId;
    public override string Name => BlessingName;

    public override void Bind(Hero hero) { }
}
