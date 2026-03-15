using System.ComponentModel.DataAnnotations;

namespace StadiumAnalytics.Infrastructure.Events;

public sealed class EventConsumerOptions
{
    public const string SectionName = "EventConsumer";

    /// <summary>
    /// Maximum time in seconds to drain remaining events from the channel on graceful shutdown.
    /// </summary>
    [Range(1, 300)]
    public int ShutdownDrainTimeoutSeconds { get; set; } = 10;
}
