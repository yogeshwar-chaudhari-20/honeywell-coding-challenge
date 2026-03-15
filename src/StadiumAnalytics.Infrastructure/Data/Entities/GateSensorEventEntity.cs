using StadiumAnalytics.Core.Models;

namespace StadiumAnalytics.Infrastructure.Data.Entities;

public sealed class GateSensorEventEntity
{
    public Guid Id { get; set; }
    public StadiumGate Gate { get; set; }

    /// <summary>
    /// Stored as ISO 8601 string for SQLite compatibility with comparison operators.
    /// EF Core's SQLite provider cannot translate DateTimeOffset comparisons in LINQ.
    /// </summary>
    public string Timestamp { get; set; } = string.Empty;
    public int NumberOfPeople { get; set; }
    public GateEventType Type { get; set; }
    public string CreatedAtUtc { get; set; } = string.Empty;
}
