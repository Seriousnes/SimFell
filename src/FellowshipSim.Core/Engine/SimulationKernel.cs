namespace FellowshipSim.Core.Engine;

/// <summary>
/// A pending event in the simulation queue. Holding the handle allows the owner to cancel it
/// (e.g. the remaining ticks of a channel that was stopped early).
/// </summary>
public sealed class ScheduledEvent
{
    internal ScheduledEvent(SimTime time, long sequence, Action<SimulationContext>? action, PendingHit? hit, string label)
    {
        Time = time;
        Sequence = sequence;
        Action = action;
        Hit = hit;
        Label = label;
    }

    public SimTime Time { get; }
    internal long Sequence { get; }
    /// <summary>What the event does; null when it is a hit's landing (<see cref="Hit"/>).</summary>
    internal Action<SimulationContext>? Action { get; }
    /// <summary>The travelling hit that lands at <see cref="Time"/>.</summary>
    internal PendingHit? Hit { get; }
    /// <summary>The event's name for debugging. Call sites pass a fixed name, such as the ability's, so scheduling builds no string.</summary>
    public string Label { get; }
    public bool IsCancelled { get; private set; }
    public bool HasFired { get; internal set; }

    public void Cancel() => IsCancelled = true;
}

/// <summary>
/// Time-ordered event queue. Events at the same timestamp fire in the order they were scheduled.
/// </summary>
public class SimulationKernel
{
    private const int InitialHeapCapacity = 64;

    // Speed: a binary heap of the events with their keys (time, then sequence) beside them, compared as two longs.
    private Entry[] _heap = new Entry[InitialHeapCapacity];
    private int _count;
    private long _sequence;

    private readonly struct Entry(long milliseconds, long sequence, ScheduledEvent scheduledEvent)
    {
        public readonly long Milliseconds = milliseconds;
        public readonly long Sequence = sequence;
        public readonly ScheduledEvent Event = scheduledEvent;

        public bool Before(in Entry other) =>
            Milliseconds < other.Milliseconds || (Milliseconds == other.Milliseconds && Sequence < other.Sequence);
    }

    /// <summary>Events in the queue, cancelled ones included until they reach the front.</summary>
    public int Count => _count;

    public ScheduledEvent ScheduleAt(SimTime time, Action<SimulationContext> action, string label = "")
    {
        var scheduledEvent = new ScheduledEvent(time, _sequence++, action, null, label);
        Push(new Entry(time.Milliseconds, scheduledEvent.Sequence, scheduledEvent));
        return scheduledEvent;
    }

    /// <summary>Schedules <paramref name="hit"/> to land at <paramref name="time"/>.</summary>
    public ScheduledEvent ScheduleAt(SimTime time, PendingHit hit, string label = "")
    {
        var scheduledEvent = new ScheduledEvent(time, _sequence++, null, hit, label);
        Push(new Entry(time.Milliseconds, scheduledEvent.Sequence, scheduledEvent));
        return scheduledEvent;
    }

    /// <summary>Time of the next live event, or null when the queue is empty.</summary>
    public SimTime? PeekNextTime()
    {
        while (_count > 0)
        {
            var scheduledEvent = _heap[0].Event;
            if (!scheduledEvent.IsCancelled) return scheduledEvent.Time;
            Pop();
        }
        return null;
    }

    /// <summary>Removes and returns the next live event due at or before <paramref name="maxTime"/>.</summary>
    public bool TryDequeueDue(SimTime maxTime, out ScheduledEvent scheduledEvent)
    {
        while (_count > 0)
        {
            scheduledEvent = _heap[0].Event;
            if (scheduledEvent.IsCancelled)
            {
                Pop();
                continue;
            }
            if (scheduledEvent.Time > maxTime) return false;
            Pop();
            return true;
        }
        scheduledEvent = null!;
        return false;
    }

    public void Clear()
    {
        Array.Clear(_heap, 0, _count);
        _count = 0;
        _sequence = 0;
    }

    private void Push(Entry entry)
    {
        if (_count == _heap.Length) Array.Resize(ref _heap, _heap.Length * 2);
        var index = _count++;
        while (index > 0)
        {
            var parent = (index - 1) >> 1;
            if (!entry.Before(_heap[parent])) break;
            _heap[index] = _heap[parent];
            index = parent;
        }
        _heap[index] = entry;
    }

    private void Pop()
    {
        var last = --_count;
        var entry = _heap[last];
        _heap[last] = default;
        if (last == 0) return;
        var index = 0;
        while (true)
        {
            var child = 2 * index + 1;
            if (child >= last) break;
            if (child + 1 < last && _heap[child + 1].Before(_heap[child])) child++;
            if (!_heap[child].Before(entry)) break;
            _heap[index] = _heap[child];
            index = child;
        }
        _heap[index] = entry;
    }
}
