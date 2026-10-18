# Adding a hero (and talents, gear and buffs)

This guide is for adding content to the engine by hand: a new hero, abilities, talents, traits, blessings, sets,
legendaries, weapons and buffs from other players. Rime (`src/FellowshipSim.Core/Heroes/Rime`) is the complete example
to copy from.

## How the engine is organised

```
src/FellowshipSim.Core/
  Domain/         Hero, Ability, Aura, resources, talents: the base classes everything builds on
  Engine/         the simulation: time, events, damage, periodic effects, procs, cooldowns
  Heroes/<Hero>/  one folder per hero: <Hero>.cs, Abilities/ (one file each), Talents/ (one file each)
  Customization/  gear shared by all heroes: Traits/, Blessings/, Gems/, Sets/, Legendaries/, Weapons/, WeaponAbilities/
  Import/         CharacterBuild (a build by game IDs) and HeroImporter (build -> hero)
  Replay/         replaying combat logs, the review page, PartyBuffs (buffs from other players)
  Catalog.cs      finds every class above by its ID: nothing to register by hand
```

Three rules hold everywhere:

1. **One class per file**, named after the class.
2. **Game values are public constants at the top of the class**: IDs, names, coefficients, cooldowns, durations,
   percentages, per-rank tables. Nothing is read from game data at runtime, so a patch that changes a number is a
   one-line edit.
3. **Write down where a value comes from** in its comment: the tooltip text, the game data record, or the combat logs it
   was measured from. Mark anything not yet checked against a log with `UNVERIFIED`.

## Where game values come from

- **The game data (datamine):** the Codex SDK exports `entities.jsonl` with every ability, effect, talent, trait,
  blessing, gem node and set: cooldowns, cast times, coefficients, effect durations, stacks and stat modifiers. Use it as
  the reference when writing constants, and compare against it when the game is patched.
- **Combat logs:** what really happens (timings, procs, partial ticks). The replay review shows where the engine and a
  log disagree.

**IDs in combat logs** are game IDs plus an offset: abilities as-is, effects (auras and effect damage) + 1,000,000,
talents + 2,000,000, traits + 3,000,000 (see `Domain/LogIds.cs`). Blessings are the exception: logs give them per-item
IDs, so they are matched by name.

## 1. The hero class

Create `Heroes/<Hero>/<Hero>.cs`, a subclass of `Hero`. The class name is the hero's name (`Elarion`); the catalog finds
it automatically, and the web page lets you replay that hero's fights once it exists.

```csharp
public sealed class Elarion : Hero
{
    /// <summary>Focus: combat logs report it as resource type N.</summary>
    public static readonly ResourceDef Focus = new("Focus", Max: 100, LogResourceType: N, LogScale: 0.01);

    public static readonly TalentSlot[] TalentTree = [ /* row, column, cost, talent ID */ ];

    public override void Initialize()
    {
        AddResource(Focus);
        SpiritRefundResource = Focus;           // Spirit Refund gives back the hero's spender resource
        AddClassTuningAura("Elarion class tuning", ClassDamageAuraHistory);   // server-side damage tuning, by date
        Abilities.Add(new FocusedShot(this));
        // ...
    }

    public override void ResetForIteration() => ResetCombatState();
}
```

Shared mechanics are already in `Hero` and need no code: Spirit Refund (chance Spirit / (1 + Spirit), Rising Spirit,
the refund arriving 0.2s later), Spirit of Heroism, the global cooldown, cooldown recovery and haste, primary stat,
crit, expertise and spirit.

## 2. Abilities

One file per ability in `Heroes/<Hero>/Abilities/`. Constants first, then auras, then the constructor and what the
ability does:

```csharp
/// <summary>Focused Shot: ... (tooltip text)</summary>
public class FocusedShot : Ability
{
    public const int AbilityId = 1315;
    public const string AbilityName = "Focused Shot";
    public const double Coefficient = 1.23;        // game data placeholder coefficient
    public const double CastTimeSeconds = 1.5;
    public const double FocusGain = 20;

    public FocusedShot(Elarion owner) : base(owner, AbilityId, AbilityName, AbilityCategory.Basic)
    {
        BaseCoefficient = Coefficient;
        CastTime = SimTime.FromSeconds(CastTimeSeconds);
        // SetCooldown(seconds, charges, scalesWithHaste) and SetChannel(duration, tickPeriod) as the game data says.
    }

    public override void Execute(SimulationContext ctx, EnemyTarget target)
    {
        Owner.GainResource(Elarion.Focus, FocusGain, ctx.CurrentTime);
        DealDamage(ctx, target);
    }
}
```

What the base class gives you:

| Need | Use |
|---|---|
| Cooldown, charges, haste-scaled cooldown | `SetCooldown(seconds, charges, scalesWithHaste)` in the constructor, `TriggerCooldown(ctx)` when cast |
| Channel | `SetChannel(duration, tickPeriod)`, then override `OnChannelTick` / `OnChannelEnd` |
| Usable during the global cooldown | `IgnoresGcd = true` |
| Can it be cast now (costs, requirements) | override `CanCast` |
| One hit | `DealDamage(ctx, target, damageMultiplier, canCrit, ..., onLanded, extraCritChance)` |
| Hits on every enemy, with sqrt(T/N) falloff beyond T targets | `AoeTargetThreshold = T`, then `DealAoeDamage(ctx, ctx.ActiveEnemies)` |
| Damage logged under an effect ID | `DamageLogId = LogIds.Effect(effectId)` |
| A hasted damage-over-time or pulsing effect | `PeriodicEffect.BeginHasted(...)` (handles haste changes and the partial final tick) |
| A buff | `public static readonly AuraDef Buff = AuraDef.Effect(name, effectId, seconds, maxStacks, modifiers)`, then `Owner.Auras.Apply(Buff, ctx.CurrentTime)` (replays find it and compare it with the log; an aura built at runtime instead needs `hero.Register(aura)`) |
| Spending or gaining a resource | `Owner.SpendResource(resource, amount, ctx)`, `Owner.GainResource(resource, amount, ctx.CurrentTime)` |
| A chance to proc | `ctx.Outcomes.RollProc(key, chance, ctx, evidenceLogId)`; pass the log ID the proc produces so a replay can follow the log |
| Procs per minute | `new RealPpm(key, ppm).Roll(hero, ctx, evidenceLogId)` |
| Something later | `ctx.Schedule(SimTime.FromSeconds(seconds), c => ..., "label")` |

Action lists (APL) refer to abilities by their name in snake case (`focused_shot`), to resources by name
(`focus`, `resource.focus`) and to buffs as `buff.<name>.stack`.

## 3. Talents

One file per talent in `Heroes/<Hero>/Talents/`. A talent hooks its effect into the hero in `Bind`, so abilities never
check which talents are taken:

```csharp
/// <summary>Talent 627: "Each pulse of Bursting Ice grants you Harrowing Ice ..." (tooltip text)</summary>
public class HarrowingIce : Talent
{
    public const int TalentId = 627;
    public const string TalentName = "Harrowing Ice";
    public const double DamagePerStack = 0.03;
    public static readonly AuraDef Buff = AuraDef.Effect("Harrowing Ice", 2839, 12.0, 31);

    public override string Name => TalentName;
    public override int Id => TalentId;

    public override void Bind(Hero hero)
    {
        var burstingIce = hero.GetAbility<BurstingIce>();
        burstingIce.PulseDamageMultipliers.Add(ctx => 1.0 + DamagePerStack * hero.Auras.GetStacks(Buff, ctx.CurrentTime));
        burstingIce.Pulsed += ctx => hero.Auras.Apply(Buff, ctx.CurrentTime);
    }
}
```

Hooks every hero has:

| Hook | Fires when |
|---|---|
| `hero.OnCastStarted`, `hero.OnCastCompleted` | a cast starts / resolves (instants resolve at once) |
| `hero.OnChannelTick`, `hero.OnChannelEnded` | a channel ticks / ends or is stopped |
| `hero.OnDamageDealt` | a hit lands (with its damage and whether it crit) |
| `hero.OnResourceSpent`, `hero.OnResourceOverflow` | resource is spent / gained past its maximum |
| `hero.AbilityDamageMultipliers`, `hero.TargetDamageMultipliers`, `hero.TargetCritChanceBonuses`, `hero.CooldownAccelerations` | read on every hit / cooldown tick |
| `hero.PassiveAuras` | always-on auras (stat bonuses) |
| `hero.GuaranteedSpiritRefunds` | effects that make the next Spirit Refund certain |

When a talent needs a moment the general hooks do not cover, give the ability or hero an event for it and raise it at
that moment (Rime: `BurstingIce.Pulsed`, `FrostBolt.Landed`, `Rime.AnimaGenerated`, `Rime.IceCometCast`). If a talent
changes how a spell works rather than adding to it (Icy Talons replacing two abilities), set a clearly named property
on the hero or ability from the talent's `Bind` and let the ability read it. An ability that replaces another and
counts as it for talents inherits from it and overrides only what differs (Rime: `TalonStrike : GlacialBlast`,
`RisingTalons : IceComet`); `hero.GetAbility<GlacialBlast>()` still returns Glacial Blast itself.

A talent that deals damage of its own keeps that inside the talent: a small private `Ability` subclass for the damage
numbers (coefficient, log ID, target falloff) and the talent's own state (Rime: `CoalescingFrost`).

Also list every talent in the hero's `TalentTree`, one `TalentSlot.InRow(row, cost, a, b, c)` per row as in game.

## 4. Gear and other content

All found by the catalog through their ID constants; add the file and it is used.

| Content | Folder | Identified by |
|---|---|---|
| Trait | `Customization/Traits` | `TraitId` (`Id`); per-rank values as `public static readonly double[] XByRank` read with `ForRank(...)` |
| Blessing | `Customization/Blessings` | `BlessingId` and its name (logs match blessings by name) |
| Gem colour | `Customization/Gems` | its `AllNodes` table (node ID, slot, required power, values) |
| Set bonus | `Customization/Sets` | `SetBonusIds` (one per main-stat variant of the set) |
| Legendary | `Customization/Legendaries` | `HeroName` and the `ItemIds` that grant it |
| Weapon | `Customization/Weapons` (+ its ability in `WeaponAbilities`) | `WeaponAbilityId` |
| Buff from another player | `Replay/PartyBuffs.cs` | the buff's effect ID (optionally scaled by the giver's trait rank) |

Something not modeled yet should still get a class (e.g. `UnmodeledTrait`) with its ID, name and tooltip text: the
review then names it as unmodeled instead of an unknown ID.

## 5. Checking against combat logs

1. Run the web page (`dotnet run --project src/FellowshipSim.Server`), paste a log with your hero and click the player.
2. Open the review. It shows damage and hits per ability against the log, every step where the engine disagreed, casts
   the engine would have refused, and anything the build uses that is not modeled.
3. Fix the biggest differences first; a whole ability off by a constant factor usually means a missing talent, buff or
   multiplier, while scattered single hits are usually timing.
4. `dotnet test` replays every downloaded fight (`data/fights`) and checks each talent of Rime's tree in a normal sim.
