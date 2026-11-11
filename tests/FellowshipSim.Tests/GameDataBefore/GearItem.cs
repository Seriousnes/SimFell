using FellowshipSim.Core.Import;

namespace FellowshipSim.Tests.GameDataBefore;

/// <summary>An item of tools/generate-gear.cs: its icon is the game's texture name.</summary>
public sealed record GearItem(int Id, string Name, GearKind Kind, int ItemLevel, int MaxTempers, bool Legendary,
    int WeaponAbilityId, GearStats Stats, string[] Dungeons, string Icon);
