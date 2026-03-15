using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StadiumAnalytics.Core.Events;
using StadiumAnalytics.Core.Models;

namespace StadiumAnalytics.Infrastructure.Simulation;

public sealed class EventSimulatorService : BackgroundService
{
    private readonly IGateEventChannel _channel;
    private readonly ILogger<EventSimulatorService> _logger;
    private readonly EventSimulationOptions _options;
    private readonly Random _random = new();

    private GateSensorEvent? _lastEvent;
    private int _tickCount;

    public EventSimulatorService(
        IGateEventChannel channel,
        IOptions<EventSimulationOptions> options,
        ILogger<EventSimulatorService> logger)
    {
        _channel = channel;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Event simulator is disabled");
            return;
        }

        _logger.LogInformation(
            "Event simulator started with {IntervalSeconds}s interval",
            _options.IntervalSeconds);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.IntervalSeconds));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await GenerateTickEventsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during event simulation tick");
            }
        }

        _logger.LogInformation("Event simulator stopped");
    }

    private async Task GenerateTickEventsAsync(CancellationToken cancellationToken)
    {
        _tickCount++;
        var wholeMinute = TruncateToMinute(DateTimeOffset.UtcNow);
        var gates = Enum.GetValues<StadiumGate>();

        foreach (var gate in gates)
        {
            var enterEvent = new GateSensorEvent(
                gate, wholeMinute, _random.Next(1, 51), GateEventType.Enter);

            var leaveEvent = new GateSensorEvent(
                gate, wholeMinute, _random.Next(1, 51), GateEventType.Leave);

            await _channel.PublishAsync(enterEvent, cancellationToken);
            await _channel.PublishAsync(leaveEvent, cancellationToken);

            _lastEvent = leaveEvent;
        }

        _logger.LogDebug(
            "Simulator tick {Tick}: published {Count} events for minute {Timestamp}",
            _tickCount, gates.Length * 2, wholeMinute);

        if (_tickCount % 10 == 0 && _lastEvent is not null)
        {
            _logger.LogDebug(
                "Injecting deliberate duplicate: Gate={Gate}, Timestamp={Timestamp}, Type={Type}",
                _lastEvent.Gate, _lastEvent.Timestamp, _lastEvent.Type);

            await _channel.PublishAsync(_lastEvent, cancellationToken);
        }
    }

    private static DateTimeOffset TruncateToMinute(DateTimeOffset value)
    {
        return new DateTimeOffset(value.Year, value.Month, value.Day, value.Hour, value.Minute, 0, TimeSpan.Zero);
    }
}
