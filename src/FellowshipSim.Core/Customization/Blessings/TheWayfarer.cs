namespace FellowshipSim.Core.Customization.Blessings;

/// <summary>
/// Blessing 703: "Every {cooldown} you gain {EffectName}, granting you +6% / +10% / +16% / +25% Haste for {duration}. Each time you use your [CORE] ability, the time to {EffectName}'s next application is reduced by 0.2 to 8 seconds, relative to the cooldown of the [CORE]ability used. Abilities with no cooldown provide 0.2 seconds reduction."
/// Not modeled yet (reported, no effect).
/// </summary>
public class TheWayfarer : UnmodeledBlessing
{
    public const int BlessingId = 703;
    public const string BlessingName = "The Wayfarer";
    public override int Id => BlessingId;
    public override string Name => BlessingName;
}
