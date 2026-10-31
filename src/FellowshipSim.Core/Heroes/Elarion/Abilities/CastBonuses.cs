namespace FellowshipSim.Core.Heroes.Elarion.Abilities;

/// <summary>The damage multiplier and crit chance talents added to a cast, for hits dealt after it resolved (a ricochet, a channel tick).</summary>
public readonly record struct CastBonuses(double Damage, double Crit);
