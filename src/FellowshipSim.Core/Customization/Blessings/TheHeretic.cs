namespace FellowshipSim.Core.Customization.Blessings;

/// <summary>
/// Blessing 708: "Each time you use a [CORE] ability, there is a {chance} chance you drain 325% / 520% / 832% / 1331% Main Stat health from the nearest enemy, healing you for the same amount."
/// Not modeled yet (reported, no effect).
/// </summary>
public class TheHeretic : UnmodeledBlessing
{
    public const int BlessingId = 708;
    public const string BlessingName = "The Heretic";
    public override int Id => BlessingId;
    public override string Name => BlessingName;
}
