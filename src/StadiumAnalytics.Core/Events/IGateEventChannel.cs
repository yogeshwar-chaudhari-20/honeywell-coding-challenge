using StadiumAnalytics.Core.Models;

namespace StadiumAnalytics.Core.Events;

public interface IGateEventChannel
{
    ValueTask PublishAsync(GateSensorEvent sensorEvent, CancellationToken cancellationToken = default);

    IAsyncEnumerable<GateSensorEvent> ReadAllAsync(CancellationToken cancellationToken = default);
}
