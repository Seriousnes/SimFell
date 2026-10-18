namespace FellowshipSim.Core.Domain;

/// <summary>
/// The definition of an aura (buff/debuff): identity, duration, stacking and stat modifiers.
/// <see cref="LogId"/> is the ID combat logs use for it (effect ID + 1,000,000), or 0 for engine-internal auras.
/// </summary>
public sealed record AuraDef(string Name, int LogId, SimTime Duration, int MaxStacks, IReadOnlyList<StatModifier> Modifiers)
{
    /// <summary>Who applied it, when the same aura from different sources stacks as separate instances (0 = one instance).</summary>
    public int SourceId { get; init; }

    /// <summary>
    /// On refresh, up to this fraction of <see cref="Duration"/> of the time left carries over (0 = a refresh restarts the duration).
    /// </summary>
    public double RefreshCarryoverFraction { get; init; }

    /// <summary>When above 0, a re-application adds this many seconds to the time left instead of restarting the duration.</summary>
    public double ReapplyExtendsSeconds { get; init; }

    public string Key => LogId > 0 ? (SourceId > 0 ? $"#{LogId}@{SourceId}" : $"#{LogId}") : Name;

    public AuraDef WithDuration(double seconds) => this with { Duration = SimTime.FromSeconds(seconds) };
    public AuraDef WithModifiers(params StatModifier[] modifiers) => this with { Modifiers = modifiers };
    public AuraDef WithMaxStacks(int maxStacks) => this with { MaxStacks = maxStacks };

    /// <summary>
    /// An aura from a game effect, logged as the effect ID + 1,000,000. A <paramref name="durationSeconds"/> of 0 lasts until
    /// removed.
    /// </summary>
    public static AuraDef Effect(string name, int effectId, double durationSeconds = 0, int maxStacks = 1, params StatModifier[] modifiers) =>
        new(name, LogIds.Effect(effectId), durationSeconds > 0 ? SimTime.FromSeconds(durationSeconds) : SimTime.Infinite, maxStacks, modifiers);

    /// <summary>An engine-internal aura that combat logs never show.</summary>
    public static AuraDef Internal(string name, double durationSeconds, int maxStacks = 1, params StatModifier[] modifiers) =>
        new(name, 0, durationSeconds <= 0 ? SimTime.Infinite : SimTime.FromSeconds(durationSeconds), maxStacks, modifiers);
}

public class Aura
{
    public Aura(AuraDef def)
    {
        Def = def;
    }

    /// <summary>An engine-internal aura by name (kept for code that has no datamine effect).</summary>
    public Aura(string name, SimTime duration, params StatModifier[] modifiers)
        : this(new AuraDef(name, 0, duration, 1, modifiers)) { }

    public Aura(string name, SimTime duration, int maxStacks, params StatModifier[] modifiers)
        : this(new AuraDef(name, 0, duration, maxStacks, modifiers)) { }

    public AuraDef Def { get; }
    public string Name => Def.Name;
    public int LogId => Def.LogId;
    public string Key => Def.Key;
    public SimTime Duration => Def.Duration;
    public int MaxStacks => Def.MaxStacks;
    public IReadOnlyList<StatModifier> Modifiers => Def.Modifiers;

    public SimTime AppliedAt { get; set; } = SimTime.Zero;
    public SimTime ExpiresAt { get; set; } = SimTime.Infinite;
    public int Stacks { get; set; } = 1;

    public bool IsExpired(SimTime now) => now >= ExpiresAt;
}

public class AuraContainer
{
    private readonly Dictionary<string, Aura> _auras = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, long> _uptimeMs = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, SimTime> _activeSince = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Raised whenever an aura is applied, stacked or refreshed (aura, stacks before, time).</summary>
    public event Action<Aura, int, SimTime>? OnGained;

    /// <summary>Raised when an aura is removed or expires.</summary>
    public event Action<Aura, SimTime>? OnRemoved;

    public IReadOnlyDictionary<string, long> UptimeMs => _uptimeMs;

    /// <summary>
    /// The game ends timed auras on the server frame after their duration runs out: across 10 Rime logs, auras last
    /// 0.006-0.026s longer than their duration (never a full 33ms frame), so on average half a frame.
    /// </summary>
    public const double ExpiryLagSeconds = 1.0 / 60.0;

    /// <summary>
    /// Applies <paramref name="def"/>: a new aura starts with <paramref name="stacks"/> stacks; an existing one is
    /// refreshed and gains <paramref name="stacks"/> (capped at max), or is set to <paramref name="setStacks"/>.
    /// </summary>
    public Aura Apply(AuraDef def, SimTime now, int stacks = 1, int? setStacks = null)
    {
        CleanExpired(now);
        var expires = def.Duration == SimTime.Infinite ? SimTime.Infinite : now + def.Duration + SimTime.FromSeconds(ExpiryLagSeconds);
        if (_auras.TryGetValue(def.Key, out var existing))
        {
            int before = existing.Stacks;
            var previousExpiry = existing.ExpiresAt;
            if (!ReferenceEquals(existing.Def, def) && existing.Def != def)
            {
                // Re-applied with different values (e.g. a bonus recomputed from current stats): the new values win.
                existing = new Aura(def) { AppliedAt = existing.AppliedAt, Stacks = existing.Stacks };
                _auras[def.Key] = existing;
            }
            if (def.ReapplyExtendsSeconds > 0 && expires != SimTime.Infinite && previousExpiry > now)
            {
                expires = previousExpiry + SimTime.FromSeconds(def.ReapplyExtendsSeconds);
            }
            else if (def.RefreshCarryoverFraction > 0 && expires != SimTime.Infinite && previousExpiry > now)
            {
                double left = (previousExpiry - now).TotalSeconds;
                expires += SimTime.FromSeconds(Math.Min(left, def.Duration.TotalSeconds * def.RefreshCarryoverFraction));
            }
            existing.ExpiresAt = expires;
            existing.Stacks = Math.Clamp(setStacks ?? (existing.Stacks + stacks), 1, existing.MaxStacks);
            OnGained?.Invoke(existing, before, now);
            return existing;
        }

        var aura = new Aura(def)
        {
            AppliedAt = now,
            ExpiresAt = expires,
            Stacks = Math.Clamp(setStacks ?? stacks, 1, def.MaxStacks)
        };
        _auras[def.Key] = aura;
        _activeSince[def.Key] = now;
        OnGained?.Invoke(aura, 0, now);
        return aura;
    }

    /// <summary>Applies an engine-internal aura built inline (no datamine definition).</summary>
    public void Apply(Aura aura, SimTime now = default, int? explicitStacks = null) =>
        Apply(aura.Def, now, aura.Stacks, explicitStacks);

    public bool HasAura(AuraDef def, SimTime now = default) => HasAura(def.Key, now);
    public int GetStacks(AuraDef def, SimTime now = default) => GetStacks(def.Key, now);
    public void Remove(AuraDef def, SimTime now = default) => Remove(def.Key, now);
    public void ConsumeStack(AuraDef def, SimTime now = default, int count = 1) => ConsumeStack(def.Key, now, count);
    public void Extend(AuraDef def, SimTime duration) => Extend(def.Key, duration);
    public Aura? Get(AuraDef def, SimTime now = default) => Get(def.Key, now);

    public Aura? Get(string key, SimTime now = default)
    {
        CleanExpired(now);
        return _auras.TryGetValue(key, out var aura) ? aura : null;
    }

    public void Extend(string key, SimTime duration)
    {
        if (_auras.TryGetValue(key, out var aura) && aura.ExpiresAt != SimTime.Infinite)
        {
            aura.ExpiresAt += duration;
        }
    }

    public void ConsumeStack(string key, SimTime now = default, int count = 1)
    {
        CleanExpired(now);
        if (_auras.TryGetValue(key, out var aura))
        {
            if (aura.Stacks > count) aura.Stacks -= count;
            else Remove(key, now);
        }
    }

    public void Remove(string key, SimTime now = default)
    {
        if (_auras.Remove(key, out var aura))
        {
            if (_activeSince.Remove(key, out var start))
            {
                var endTime = aura.ExpiresAt != SimTime.Infinite && aura.ExpiresAt < now ? aura.ExpiresAt : now;
                long dur = Math.Max(0, (endTime - start).Milliseconds);
                _uptimeMs[key] = _uptimeMs.GetValueOrDefault(key, 0L) + dur;
            }
            OnRemoved?.Invoke(aura, now);
        }
    }

    public bool HasAura(string key, SimTime now = default)
    {
        CleanExpired(now);
        return _auras.ContainsKey(key);
    }

    public int GetStacks(string key, SimTime now = default)
    {
        CleanExpired(now);
        return _auras.TryGetValue(key, out var aura) ? aura.Stacks : 0;
    }

    public IEnumerable<Aura> Active(SimTime now = default)
    {
        CleanExpired(now);
        return _auras.Values;
    }

    public Dictionary<string, int> GetActiveAuraStacks(SimTime now = default)
    {
        CleanExpired(now);
        return _auras.ToDictionary(kv => kv.Key, kv => kv.Value.Stacks, StringComparer.OrdinalIgnoreCase);
    }

    public void CleanExpired(SimTime now)
    {
        if (now == default) return;
        List<Aura>? expired = null;
        foreach (var aura in _auras.Values)
        {
            if (aura.IsExpired(now))
            {
                (expired ??= new()).Add(aura);
            }
        }
        if (expired == null) return;
        foreach (var aura in expired)
        {
            Remove(aura.Key, aura.ExpiresAt);
        }
    }

    /// <summary>The earliest time after <paramref name="now"/> an active aura runs out (Infinite if none does).</summary>
    public SimTime NextExpiry(SimTime now)
    {
        var next = SimTime.Infinite;
        foreach (var aura in _auras.Values)
        {
            if (aura.ExpiresAt > now && aura.ExpiresAt < next) next = aura.ExpiresAt;
        }
        return next;
    }

    /// <summary>Applies every active modifier of <paramref name="type"/> to <paramref name="baseValue"/>.</summary>
    public double Fold(StatType type, double baseValue, SimTime now = default)
    {
        CleanExpired(now);
        var fold = new ModifierFold();
        foreach (var aura in _auras.Values)
        {
            foreach (var mod in aura.Modifiers)
            {
                if (mod.Type == type) fold.Accept(mod, aura.Stacks);
            }
        }
        return fold.Apply(baseValue);
    }

    /// <summary>Sum of additive modifiers of <paramref name="type"/>.</summary>
    public double GetTotalModifier(StatType type, SimTime now = default) => Fold(type, 0.0, now);

    public void ClearAll()
    {
        _auras.Clear();
        _activeSince.Clear();
    }

    public void FinalizeUptimes(SimTime combatEndTime)
    {
        foreach (var (key, start) in _activeSince)
        {
            long dur = Math.Max(0, (combatEndTime - start).Milliseconds);
            _uptimeMs[key] = _uptimeMs.GetValueOrDefault(key, 0L) + dur;
        }
        _activeSince.Clear();
    }

    public double GetUptimePercent(string key, SimTime totalCombatDuration)
    {
        if (totalCombatDuration.Milliseconds <= 0) return 0.0;
        return (double)_uptimeMs.GetValueOrDefault(key, 0L) / totalCombatDuration.Milliseconds * 100.0;
    }
}
