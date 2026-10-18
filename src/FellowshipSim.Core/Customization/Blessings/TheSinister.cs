namespace FellowshipSim.Core.Customization.Blessings;

/// <summary>
/// Blessing 701: "Your [CORE] abilities have 1% / 2% / 3% / 4% increased Critical Strike chance and 5% / 10% / 15% / 20% increased Critical Strike power."
/// Not modeled yet (reported, no effect).
/// </summary>
public class TheSinister : UnmodeledBlessing
{
    public const int BlessingId = 701;
    public const string BlessingName = "The Sinister";
    public override int Id => BlessingId;
    public override string Name => BlessingName;
}
