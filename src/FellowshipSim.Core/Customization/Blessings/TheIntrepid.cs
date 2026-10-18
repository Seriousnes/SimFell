namespace FellowshipSim.Core.Customization.Blessings;

/// <summary>
/// Blessing 711: "Your [BASIC] abilities increases the power of your next [POWER] ability by 1.5% / 2.5% / 4.0% / 6.0%, stacking up to 5 times."
/// Not modeled yet (reported, no effect).
/// </summary>
public class TheIntrepid : UnmodeledBlessing
{
    public const int BlessingId = 711;
    public const string BlessingName = "The Intrepid";
    public override int Id => BlessingId;
    public override string Name => BlessingName;
}
