using FellowshipSim.Core.Domain;
using FellowshipSim.Core.Engine;

namespace FellowshipSim.Core.Customization.Sets;

/// <summary>
/// Set bonus 681 / set-bonus-power 246 / effect 1994: "Your abilities have a chance (0.8 PPM) to increase your Haste
/// by 25% for 20 seconds." Rolled on every ability used.
/// </summary>
public class DarkProphecy : SetBonus
{
    public const int EffectId = 1994;
    public const int PowerId = 246;
    public const double DurationSeconds = 20.0;
    public const string ProcKey = "dark_prophecy";
    public override string Name => "Dark Prophecy";

    public override void Bind(Hero hero)
    {
        var aura = hero.DefineAura(EffectId);
        if (aura.Duration == SimTime.Infinite) aura = aura.WithDuration(DurationSeconds);
        var ppm = System.Text.RegularExpressions.Regex.Match(hero.Data.Raw("set-bonus-power", PowerId).GetProperty("description").GetString()!, @"\(([\d.]+) PPM\)");
        var rppm = new RealPpm(ProcKey, double.Parse(ppm.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture));
        hero.OnCastCompleted += (ability, _, now) =>
        {
            if (hero.SimContext is { } ctx && rppm.Roll(hero, ctx, aura.LogId)) hero.Auras.Apply(aura, now);
        };
    }
}
