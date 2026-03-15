using StadiumAnalytics.Core.Models;

namespace StadiumAnalytics.Core.Dtos;

public sealed class SensorEventQuery
{
    public StadiumGate? Gate { get; init; }
    public GateEventType? Type { get; init; }
    public DateTimeOffset? StartTime { get; init; }
    public DateTimeOffset? EndTime { get; init; }

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();

        if (StartTime.HasValue && EndTime.HasValue && StartTime.Value > EndTime.Value)
            errors.Add("startTime must be less than or equal to endTime.");

        return errors;
    }
}
