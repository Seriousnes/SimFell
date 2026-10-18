using FellowshipSim.Core.Codex;
using FellowshipSim.Core.Domain;
using FellowshipSim.Core.Engine;
using FellowshipSim.Core.Heroes.Rime.Talents;

namespace FellowshipSim.Core.Heroes.Rime.Abilities;

/// <summary>Effect 1365 (part of Flight of the Navir): the circling swallows commanded by Cold Snap and Freezing Torrent.</summary>
public class CommandedFrostSwallows : FrostSwallowAttack
{
    public const int EffectId = 1365;

    public CommandedFrostSwallows(Rime owner)
        : base(owner, GameData.EffectLogId(EffectId), "Frost Swallows", CoefficientOf(owner.Data.Ability(FlightOfTheNavir.AbilityId))) { }

    private int _inFlight;

    /// <summary>
    /// The circling swallows not out attacking: one that is sent comes back when it hits (combat logs: a Cold Snap cast
    /// while a Freezing Torrent swallow is still flying sends only 4, yMAZVJL9hTpFjqbC fight 7 @ 51.956).
    /// </summary>
    public int Available => Math.Max(0, FlightOfTheNavir.SwallowCount - _inFlight);

    /// <summary>Sends one circling swallow at <paramref name="target"/>, if one is available.</summary>
    public bool Send(SimulationContext ctx, EnemyTarget target)
    {
        if (Available == 0) return false;
        _inFlight++;
        DealDamage(ctx, target, onLanded: (_, _) => _inFlight = Math.Max(0, _inFlight - 1));
        return true;
    }

    public void ResetState() => _inFlight = 0;
}
