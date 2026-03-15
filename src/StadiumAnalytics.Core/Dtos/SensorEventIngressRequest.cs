namespace StadiumAnalytics.Core.Dtos;

/// <summary>
/// Request body for external sensor event ingestion (matches sensor payload format).
/// </summary>
public sealed class SensorEventIngressRequest
{
    public string? Gate { get; init; }
    public DateTimeOffset? Timestamp { get; init; }
    public int? NumberOfPeople { get; init; }
    public string? Type { get; init; }
}
