using FellowshipSim.Core.Domain;
using FellowshipSim.Core.Engine;

namespace FellowshipSim.Core.Customization.Sets;

/// <summary>
/// Set bonus 686 / effect 1988: +5% Intellect (in the character sheet's Intellect when imported from a log;
/// applied otherwise). The low-health heal is defensive.
/// </summary>
public class TormentOfBaelAurum : SetBonus
{
    public const int EffectId = 1988;
    public override string Name => "Torment of Bael'Aurum";
    public override IReadOnlyList<string> UnmodeledParts => ["low-health heal"];

    public override void Bind(Hero hero)
    {
        if (!hero.Stats.UsesSheetValues) hero.PassiveAuras.Add(AuraDef.FromEffect(hero.Data, EffectId));
    }
}
