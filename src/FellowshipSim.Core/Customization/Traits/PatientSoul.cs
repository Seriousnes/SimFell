namespace FellowshipSim.Core.Customization.Traits;

/// <summary>
/// Trait 46: "Standing still for 3 seconds grants you Patient Soul, increasing your Max Health by 5% and your Expertise
/// rating by 12 / 22 / 32 / 42. Patient Soul persists for 6 seconds after you start moving." The engine does not
/// simulate movement: a sim keeps it up, a replay follows the log (situational aura). UNVERIFIED: no log has this trait;
/// the game data has two Patient Soul effects (2149, 2150) and the first is assumed.
/// </summary>
public class PatientSoul : Trait
{
    public const int TraitId = 46;
    public const string TraitName = "Patient Soul";
    public const int BuffEffectId = 2149;
    public static readonly double[] ExpertiseRatingByRank = [12, 22, 32, 42];

    public static readonly AuraDef Buff = AuraDef.Effect("Patient Soul", BuffEffectId);

    public override int Id => TraitId;
    public override string Name => TraitName;

    public override void Bind(Hero hero) =>
        hero.SituationalAuras.Add(Buff.WithModifiers(StatModifier.Add(StatType.ExpertiseRating, ForRank(ExpertiseRatingByRank))));
}
