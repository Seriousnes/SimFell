namespace FellowshipSim.Core.Customization.Blessings;

/// <summary>
/// Blessing 702: "Your [POWER] abilities have a 5% / 8% / 13% / 20% chance to generate 1 Spirit Point toward your SPIRIT ability."
/// Not modeled yet (reported, no effect).
/// </summary>
public class TheCelestial : UnmodeledBlessing
{
    public const int BlessingId = 702;
    public const string BlessingName = "The Celestial";
    public override int Id => BlessingId;
    public override string Name => BlessingName;
}
