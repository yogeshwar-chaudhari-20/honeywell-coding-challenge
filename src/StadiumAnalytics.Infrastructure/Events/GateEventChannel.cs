using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StadiumAnalytics.Core.Events;
using StadiumAnalytics.Core.Models;

namespace StadiumAnalytics.Infrastructure.Events;

public sealed class GateEventChannel : IGateEventChannel
{
    private readonly Channel<GateSensorEvent> _channel;
    private readonly ILogger<GateEventChannel> _logger;
    private readonly int _capacity;
    private readonly int _highWaterMark;

    public GateEventChannel(
        IOptions<EventChannelOptions> options,
        ILogger<GateEventChannel> logger)
    {
        _logger = logger;
        var opts = options.Value;
        _capacity = Math.Clamp(opts.Capacity, 1, 100_000);
        _highWaterMark = Math.Min(Math.Clamp(opts.HighWaterMark, 1, 100_000), _capacity);
        _channel = Channel.CreateBounded<GateSensorEvent>(new BoundedChannelOptions(_capacity)
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
        if (currentCount >= _highWaterMark)
        {
            _logger.LogWarning("Channel high-water mark reached: {Count}/{Capacity}", currentCount, _capacity);
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
