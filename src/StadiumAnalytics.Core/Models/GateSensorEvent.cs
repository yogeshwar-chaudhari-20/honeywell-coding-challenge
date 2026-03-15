namespace StadiumAnalytics.Core.Models;

public sealed record GateSensorEvent
{
    public StadiumGate Gate { get; }
    public DateTimeOffset Timestamp { get; }
    public int NumberOfPeople { get; }
    public GateEventType Type { get; }

    public GateSensorEvent(StadiumGate gate, DateTimeOffset timestamp, int numberOfPeople, GateEventType type)
    {
        if (numberOfPeople <= 0)
            throw new ArgumentOutOfRangeException(nameof(numberOfPeople), numberOfPeople, "Must be greater than zero.");

        if (!Enum.IsDefined(type))
            throw new ArgumentOutOfRangeException(nameof(type), type, "Invalid event type.");

        if (!Enum.IsDefined(gate))
            throw new ArgumentOutOfRangeException(nameof(gate), gate, "Invalid gate.");

        Gate = gate;
        Timestamp = timestamp;
        NumberOfPeople = numberOfPeople;
        Type = type;
    }
}
