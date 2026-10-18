using FellowshipSim.Core.Codex;
using FellowshipSim.Core.Domain;
using FellowshipSim.Core.Engine;

namespace FellowshipSim.Core.Customization.Blessings;

/// <summary>Blessing 706: POWER abilities gain X% power per 3% Spirit, up to 50% Spirit.</summary>
public class TheMystic : Blessing
{
    public override string Name => "The Mystic";

    public override void Bind(Hero hero)
    {
        double perStep = DescriptionValue(hero);
        double step = RankData.BlessingConstant(hero.Data, Name, "SpiritAmountPerPowerInPercentage", Rank);
        double threshold = RankData.BlessingConstant(hero.Data, Name, "SpiritThreshold", Rank);
        hero.AbilityDamageMultipliers.Add((ability, now) =>
            ability.Category == "Power"
                ? 1.0 + perStep * (Math.Min(hero.SpiritPercent(now), threshold) * 100.0 / step)
                : 1.0);
    }
}
