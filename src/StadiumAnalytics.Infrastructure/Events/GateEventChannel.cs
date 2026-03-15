using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using StadiumAnalytics.Core.Events;
using StadiumAnalytics.Core.Models;

namespace StadiumAnalytics.Infrastructure.Events;

public sealed class GateEventChannel : IGateEventChannel
{
    private const int Capacity = 1000;
    private const int HighWaterMark = 800;

    private readonly Channel<GateSensorEvent> _channel;
    private readonly ILogger<GateEventChannel> _logger;

    public GateEventChannel(ILogger<GateEventChannel> logger)
    {
        _logger = logger;
        _channel = Channel.CreateBounded<GateSensorEvent>(new BoundedChannelOptions(Capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });
    }

    public async ValueTask PublishAsync(GateSensorEvent sensorEvent, CancellationToken cancellationToken = default)
    {
        await _channel.Writer.WriteAsync(sensorEvent, cancellationToken);

        var currentCount = _channel.Reader.Count;
        if (currentCount >= HighWaterMark)
        {
            _logger.LogWarning("Channel high-water mark reached: {Count}/{Capacity}", currentCount, Capacity);
        }
    }

    public async IAsyncEnumerable<GateSensorEvent> ReadAllAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var item in _channel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return item;
        }
    }

    public int CurrentCount => _channel.Reader.Count;

    public void Complete() => _channel.Writer.Complete();
}
