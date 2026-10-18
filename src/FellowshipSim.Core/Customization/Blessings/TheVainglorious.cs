namespace FellowshipSim.Core.Customization.Blessings;

/// <summary>
/// Blessing 707: "Your BASIC abilities deal an additional 25% / 40% / 65% / 100% Main Stat damage."
/// Not modeled yet (reported, no effect).
/// </summary>
public class TheVainglorious : UnmodeledBlessing
{
    public const int BlessingId = 707;
    public const string BlessingName = "The Vainglorious";
    public override int Id => BlessingId;
    public override string Name => BlessingName;
}
