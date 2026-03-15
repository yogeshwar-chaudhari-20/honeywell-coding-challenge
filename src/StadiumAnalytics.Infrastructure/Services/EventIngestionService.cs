using StadiumAnalytics.Core.Dtos;
using StadiumAnalytics.Core.Models;
using StadiumAnalytics.Core.Services;

namespace StadiumAnalytics.Infrastructure.Services;

public sealed class EventIngestionService : IEventIngestionService
{
    public EventIngestionResult ValidateAndMap(SensorEventIngressRequest request)
    {
        if (request is null)
            return new EventIngestionResult(false, null, new[] { (string.Empty, "Request body is required.") });

        var errors = new List<(string Key, string Message)>();
        StadiumGate? parsedGate = null;
        GateEventType? parsedType = null;

        if (string.IsNullOrWhiteSpace(request.Gate))
        {
            errors.Add((nameof(request.Gate), "Gate is required."));
        }
        else if (request.Gate.Length > 100)
        {
            errors.Add((nameof(request.Gate), "Gate must not exceed 100 characters."));
        }
        else if (!StadiumGateExtensions.TryParseDisplayName(request.Gate, out var g))
        {
            var validGates = string.Join(", ", Enum.GetValues<StadiumGate>().Select(x => x.ToDisplayName()));
            errors.Add((nameof(request.Gate), $"Invalid gate. Valid values: {validGates}"));
        }
        else
        {
            parsedGate = g;
        }

        if (request.Timestamp is null)
            errors.Add((nameof(request.Timestamp), "Timestamp is required."));

        if (request.NumberOfPeople is null)
        {
            errors.Add((nameof(request.NumberOfPeople), "NumberOfPeople is required."));
        }
        else if (request.NumberOfPeople <= 0)
        {
            errors.Add((nameof(request.NumberOfPeople), "NumberOfPeople must be greater than zero."));
        }

        if (string.IsNullOrWhiteSpace(request.Type))
        {
            errors.Add((nameof(request.Type), "Type is required."));
        }
        else if (!Enum.TryParse<GateEventType>(request.Type, ignoreCase: true, out var t))
        {
            errors.Add((nameof(request.Type), "Invalid type. Valid values: enter, leave"));
        }
        else
        {
            parsedType = t;
        }

        if (errors.Count > 0)
            return new EventIngestionResult(false, null, errors);

        var sensorEvent = new GateSensorEvent(
            parsedGate!.Value,
            request.Timestamp!.Value,
            request.NumberOfPeople!.Value,
            parsedType!.Value);

        return new EventIngestionResult(true, sensorEvent, Array.Empty<(string, string)>());
    }
}
