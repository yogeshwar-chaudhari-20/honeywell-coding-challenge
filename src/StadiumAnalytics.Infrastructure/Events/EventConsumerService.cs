using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StadiumAnalytics.Core.Events;
using StadiumAnalytics.Core.Models;
using StadiumAnalytics.Infrastructure.Data;
using StadiumAnalytics.Infrastructure.Data.Entities;

namespace StadiumAnalytics.Infrastructure.Events;

public sealed class EventConsumerService : BackgroundService
{
    private readonly IGateEventChannel _channel;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EventConsumerService> _logger;

    private static readonly TimeSpan ShutdownDrainTimeout = TimeSpan.FromSeconds(10);

    public EventConsumerService(
        IGateEventChannel channel,
        IServiceScopeFactory scopeFactory,
        ILogger<EventConsumerService> logger)
    {
        _channel = channel;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Event consumer started");

        try
        {
            await foreach (var sensorEvent in _channel.ReadAllAsync(stoppingToken))
            {
                await ProcessEventAsync(sensorEvent);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Event consumer received shutdown signal, draining remaining events...");
            await DrainRemainingEventsAsync();
        }

        _logger.LogInformation("Event consumer stopped");
    }

    private async Task DrainRemainingEventsAsync()
    {
        using var drainCts = new CancellationTokenSource(ShutdownDrainTimeout);
        var drained = 0;

        try
        {
            await foreach (var sensorEvent in _channel.ReadAllAsync(drainCts.Token))
            {
                await ProcessEventAsync(sensorEvent);
                drained++;
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Drain timeout reached after processing {Drained} events", drained);
            return;
        }

        _logger.LogInformation("Graceful shutdown: drained {Drained} remaining events", drained);
    }

    private async Task ProcessEventAsync(GateSensorEvent sensorEvent)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<StadiumDbContext>();

            var entity = new GateSensorEventEntity
            {
                Id = Guid.NewGuid(),
                Gate = sensorEvent.Gate,
                Timestamp = sensorEvent.Timestamp.ToUniversalTime().ToString("o"),
                NumberOfPeople = sensorEvent.NumberOfPeople,
                Type = sensorEvent.Type,
                CreatedAtUtc = DateTimeOffset.UtcNow.ToString("o")
            };

            dbContext.GateSensorEvents.Add(entity);
            await dbContext.SaveChangesAsync();

            _logger.LogDebug(
                "Event persisted: Gate={Gate}, Timestamp={Timestamp}, Type={Type}, NumberOfPeople={NumberOfPeople}",
                sensorEvent.Gate, sensorEvent.Timestamp, sensorEvent.Type, sensorEvent.NumberOfPeople);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            _logger.LogWarning(
                "Duplicate event detected: Gate={Gate}, Timestamp={Timestamp}, Type={Type}",
                sensorEvent.Gate, sensorEvent.Timestamp, sensorEvent.Type);

            await RecordFailedEventAsync(sensorEvent, "Duplicate", "Composite key (Gate, Timestamp, Type) already exists");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to process event: Gate={Gate}, Timestamp={Timestamp}, Type={Type}",
                sensorEvent.Gate, sensorEvent.Timestamp, sensorEvent.Type);

            await RecordFailedEventAsync(sensorEvent, "PersistenceError", Truncate(ex.ToString(), 2000));
        }
    }

    private async Task RecordFailedEventAsync(GateSensorEvent sensorEvent, string reason, string? errorDetails)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<StadiumDbContext>();

            var rawPayload = JsonSerializer.Serialize(new
            {
                gate = sensorEvent.Gate.ToDisplayName(),
                timestamp = sensorEvent.Timestamp,
                numberOfPeople = sensorEvent.NumberOfPeople,
                type = sensorEvent.Type.ToString().ToLowerInvariant()
            });

            dbContext.FailedEvents.Add(new FailedEventEntity
            {
                Id = Guid.NewGuid(),
                Gate = sensorEvent.Gate.ToDisplayName(),
                Timestamp = sensorEvent.Timestamp.ToString("o"),
                NumberOfPeople = sensorEvent.NumberOfPeople,
                Type = sensorEvent.Type.ToString().ToLowerInvariant(),
                RawPayload = rawPayload,
                Reason = reason,
                ErrorDetails = errorDetails,
                FailedAtUtc = DateTimeOffset.UtcNow
            });

            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record failed event to FailedEvents table");
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        var message = ex.InnerException?.Message ?? ex.Message;
        return message.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase)
            || message.Contains("unique", StringComparison.OrdinalIgnoreCase);
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (value is null || value.Length <= maxLength)
            return value;
        return value[..maxLength];
    }
}
