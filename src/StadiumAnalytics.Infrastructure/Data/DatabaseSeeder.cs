using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StadiumAnalytics.Core.Models;
using StadiumAnalytics.Infrastructure.Data.Entities;

namespace StadiumAnalytics.Infrastructure.Data;

public sealed class DatabaseSeeder
{
    private readonly StadiumDbContext _dbContext;
    private readonly ILogger<DatabaseSeeder> _logger;

    private const int SeedMinutes = 5;
    private const int MinPeople = 1;
    private const int MaxPeoplePlusOne = 51;

    public DatabaseSeeder(StadiumDbContext dbContext, ILogger<DatabaseSeeder> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var random = new Random();
        var gates = Enum.GetValues<StadiumGate>();
        var types = Enum.GetValues<GateEventType>();
        var now = DateTimeOffset.UtcNow;
        var currentMinute = TruncateToMinute(now);

        var seeded = 0;
        var skipped = 0;

        for (var i = SeedMinutes; i >= 1; i--)
        {
            var minuteTimestamp = currentMinute.AddMinutes(-i);

            foreach (var gate in gates)
            {
                foreach (var type in types)
                {
                    var timestampStr = minuteTimestamp.ToUniversalTime().ToString("o");

                    var exists = await _dbContext.GateSensorEvents.AnyAsync(
                        e => e.Gate == gate && e.Timestamp == timestampStr && e.Type == type,
                        cancellationToken);

                    if (exists)
                    {
                        skipped++;
                        continue;
                    }

                    _dbContext.GateSensorEvents.Add(new GateSensorEventEntity
                    {
                        Id = Guid.NewGuid(),
                        Gate = gate,
                        Timestamp = timestampStr,
                        NumberOfPeople = random.Next(MinPeople, MaxPeoplePlusOne),
                        Type = type,
                        CreatedAtUtc = DateTimeOffset.UtcNow.ToString("o")
                    });

                    seeded++;
                }
            }
        }

        if (seeded > 0)
            await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded {Count} events for the past {Minutes} minutes", seeded, SeedMinutes);

        if (skipped > 0)
            _logger.LogDebug("Seed data already exists, skipped {Skipped} duplicates", skipped);
    }

    private static DateTimeOffset TruncateToMinute(DateTimeOffset value)
    {
        return new DateTimeOffset(value.Year, value.Month, value.Day, value.Hour, value.Minute, 0, value.Offset);
    }
}
