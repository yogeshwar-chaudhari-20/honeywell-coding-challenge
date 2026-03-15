namespace StadiumAnalytics.Infrastructure.Data.Entities;

public sealed class FailedEventEntity
{
    public Guid Id { get; set; }
    public string? Gate { get; set; }
    public string? Timestamp { get; set; }
    public int? NumberOfPeople { get; set; }
    public string? Type { get; set; }
    public string RawPayload { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? ErrorDetails { get; set; }
    public DateTimeOffset FailedAtUtc { get; set; }
}
