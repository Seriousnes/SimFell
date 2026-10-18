namespace FellowshipSim.Core.Customization.Blessings;

/// <summary>
/// Blessing not in the current game data (its ID is unknown).
/// Not modeled yet (reported, no effect).
/// </summary>
public class TheExecutioner : UnmodeledBlessing
{
    public const int BlessingId = 0;
    public const string BlessingName = "The Executioner";
    public override int Id => BlessingId;
    public override string Name => BlessingName;
}
