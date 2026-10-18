namespace FellowshipSim.Core.Domain;

public enum ResourceType
{
    Anima,
    Spirit,
    WinterOrbs,
    Energy,
    Mana
}

public class ResourceState
{
    public double Max { get; set; }
    public double Current { get; set; }
    public double Initial { get; set; }

    public ResourceState(double max, double initial)
    {
        Max = max;
        Current = initial;
        Initial = initial;
    }

    public void Reset() => Current = Initial;
}

public class ResourcePool
{
    private readonly Dictionary<ResourceType, ResourceState> _resources = new();

    public void Register(ResourceType type, double max, double initial)
    {
        _resources[type] = new ResourceState(max, initial);
    }

    public void Add(ResourceType type, double amount)
    {
        if (_resources.TryGetValue(type, out var res))
        {
            res.Current = Math.Min(res.Max, res.Current + amount);
        }
    }

    public bool Consume(ResourceType type, double amount)
    {
        if (_resources.TryGetValue(type, out var res) && res.Current >= amount)
        {
            res.Current -= amount;
            return true;
        }
        return false;
    }

    public ResourceState Get(ResourceType type)
    {
        if (!_resources.TryGetValue(type, out var res))
        {
            res = new ResourceState(100, 0);
            _resources[type] = res;
        }
        return res;
    }

    public void Reset()
    {
        foreach (var res in _resources.Values)
        {
            res.Reset();
        }
    }

    public Dictionary<ResourceType, double> CreateSnapshot()
    {
        var dict = new Dictionary<ResourceType, double>();
        foreach (var (k, v) in _resources)
        {
            dict[k] = v.Current;
        }
        return dict;
    }

    public void RestoreSnapshot(Dictionary<ResourceType, double> snapshot)
    {
        foreach (var (k, v) in snapshot)
        {
            if (_resources.TryGetValue(k, out var state))
            {
                state.Current = v;
            }
            else
            {
                _resources[k] = new ResourceState(100, v);
            }
        }
    }
}

