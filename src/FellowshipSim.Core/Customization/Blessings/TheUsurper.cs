namespace FellowshipSim.Core.Customization.Blessings;

/// <summary>
/// Blessing 710: "Your CORE abilities have a chance equal to your Critical Strike chance % to trigger The Usurper's Verdict on the target, either dealing 143% / 228% / 365% / 584% Main Stat damage to up to 4 enemies near your target, or healing up to 4 allies for 122% / 195% / 312% / 500% Main Stat health. The Usurper's Verdict has a 20% chance to trigger both the Damage and Healing effect."
/// Not modeled yet (reported, no effect).
/// </summary>
public class TheUsurper : UnmodeledBlessing
{
    public const int BlessingId = 710;
    public const string BlessingName = "The Usurper";
    public override int Id => BlessingId;
    public override string Name => BlessingName;
}
