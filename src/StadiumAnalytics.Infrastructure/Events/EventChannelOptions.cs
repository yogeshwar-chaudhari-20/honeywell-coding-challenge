using System.ComponentModel.DataAnnotations;

namespace StadiumAnalytics.Infrastructure.Events;

public sealed class EventChannelOptions
{
    public const string SectionName = "EventChannel";

    [Range(1, 100_000)]
    public int Capacity { get; set; } = 1000;

    /// <summary>
    /// When the number of queued items reaches this value, a high-water mark warning is logged.
    /// Should be less than or equal to Capacity.
    /// </summary>
    [Range(1, 100_000)]
    public int HighWaterMark { get; set; } = 800;
}
