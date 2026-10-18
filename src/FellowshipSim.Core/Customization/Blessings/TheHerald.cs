namespace FellowshipSim.Core.Customization.Blessings;

/// <summary>Blessing 699: "You start dungeons with  12 / 20 / 30 / 50 Spirit Points toward your SPIRIT ability." No effect on damage.</summary>
public class TheHerald : Blessing
{
    public const int BlessingId = 699;
    public const string BlessingName = "The Herald";
    public override int Id => BlessingId;
    public override string Name => BlessingName;
    public override void Bind(Hero hero) { }
}
