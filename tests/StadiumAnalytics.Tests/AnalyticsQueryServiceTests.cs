using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StadiumAnalytics.Core.Dtos;
using StadiumAnalytics.Core.Models;
using StadiumAnalytics.Core.Services;
using StadiumAnalytics.Infrastructure.Data;
using StadiumAnalytics.Infrastructure.Data.Entities;
using StadiumAnalytics.Infrastructure.Services;

namespace StadiumAnalytics.Tests;

public class AnalyticsQueryServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;

    public AnalyticsQueryServiceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
    }

    private static StadiumDbContext CreateContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<StadiumDbContext>()
            .UseSqlite(connection)
            .Options;
        return new StadiumDbContext(options);
    }

    [Fact]
    public void TryParseSummaryQuery_ValidParams_ReturnsQueryAndNoErrors()
    {
        using var db = CreateContext(_connection);
        var service = new AnalyticsQueryService(db);

        var result = service.TryParseSummaryQuery("Gate A", "enter", null, null);

        Assert.Empty(result.Errors);
        Assert.NotNull(result.Query);
        Assert.Equal(StadiumGate.GateA, result.Query!.Gate);
        Assert.Equal(GateEventType.Enter, result.Query.Type);
    }

    [Fact]
    public void TryParseSummaryQuery_InvalidGate_ReturnsErrors()
    {
        using var db = CreateContext(_connection);
        var service = new AnalyticsQueryService(db);

        var result = service.TryParseSummaryQuery("Gate Z", null, null, null);

        Assert.NotEmpty(result.Errors);
        Assert.Null(result.Query);
        Assert.Contains(result.Errors, e => e.Key == "gate" && e.Message.Contains("Valid values"));
    }

    [Fact]
    public void TryParseSummaryQuery_InvalidType_ReturnsErrors()
    {
        using var db = CreateContext(_connection);
        var service = new AnalyticsQueryService(db);

        var result = service.TryParseSummaryQuery(null, "jump", null, null);

        Assert.NotEmpty(result.Errors);
        Assert.Null(result.Query);
        Assert.Contains(result.Errors, e => e.Key == "type");
    }

    [Fact]
    public void TryParseSummaryQuery_GateTooLong_ReturnsErrors()
    {
        using var db = CreateContext(_connection);
        var service = new AnalyticsQueryService(db);

        var result = service.TryParseSummaryQuery(new string('x', 101), null, null, null);

        Assert.NotEmpty(result.Errors);
        Assert.Contains(result.Errors, e => e.Message.Contains("100 characters"));
    }

    [Fact]
    public void TryParseSummaryQuery_StartTimeAfterEndTime_ReturnsErrors()
    {
        using var db = CreateContext(_connection);
        var service = new AnalyticsQueryService(db);
        var start = DateTimeOffset.UtcNow;
        var end = start.AddHours(-1);

        var result = service.TryParseSummaryQuery(null, null, start, end);

        Assert.NotEmpty(result.Errors);
        Assert.Contains(result.Errors, e => e.Message.Contains("startTime"));
    }

    [Fact]
    public void TryParseSummaryQuery_AllOptional_ReturnsQueryWithNulls()
    {
        using var db = CreateContext(_connection);
        var service = new AnalyticsQueryService(db);

        var result = service.TryParseSummaryQuery(null, null, null, null);

        Assert.Empty(result.Errors);
        Assert.NotNull(result.Query);
        Assert.Null(result.Query!.Gate);
        Assert.Null(result.Query.Type);
    }

    [Fact]
    public async Task GetSummaryAsync_WithSeedData_ReturnsGroupedResults()
    {
        await using var db = CreateContext(_connection);
        await db.Database.EnsureCreatedAsync();

        var ts = DateTimeOffset.UtcNow.AddMinutes(-1).ToUniversalTime().ToString("o");
        var createdAt = DateTimeOffset.UtcNow.ToString("o");
        db.GateSensorEvents.AddRange(
            new GateSensorEventEntity { Id = Guid.NewGuid(), Gate = StadiumGate.GateA, Timestamp = ts, NumberOfPeople = 10, Type = GateEventType.Enter, CreatedAtUtc = createdAt },
            new GateSensorEventEntity { Id = Guid.NewGuid(), Gate = StadiumGate.GateA, Timestamp = ts, NumberOfPeople = 5, Type = GateEventType.Leave, CreatedAtUtc = createdAt });
        await db.SaveChangesAsync();

        var service = new AnalyticsQueryService(db);
        var query = new SensorEventQuery();

        var results = await service.GetSummaryAsync(query);

        Assert.NotEmpty(results);
        var gateAEnter = results.FirstOrDefault(r => r.Gate == "Gate A" && r.Type == "enter");
        Assert.NotNull(gateAEnter);
        Assert.Equal(10, gateAEnter.NumberOfPeople);
        var gateALeave = results.FirstOrDefault(r => r.Gate == "Gate A" && r.Type == "leave");
        Assert.NotNull(gateALeave);
        Assert.Equal(5, gateALeave.NumberOfPeople);
    }

    [Fact]
    public async Task GetSummaryAsync_FilterByGate_ReturnsOnlyThatGate()
    {
        await using var db = CreateContext(_connection);
        await db.Database.EnsureCreatedAsync();

        var ts = DateTimeOffset.UtcNow.ToUniversalTime().ToString("o");
        var createdAt = DateTimeOffset.UtcNow.ToString("o");
        db.GateSensorEvents.AddRange(
            new GateSensorEventEntity { Id = Guid.NewGuid(), Gate = StadiumGate.GateA, Timestamp = ts, NumberOfPeople = 1, Type = GateEventType.Enter, CreatedAtUtc = createdAt },
            new GateSensorEventEntity { Id = Guid.NewGuid(), Gate = StadiumGate.GateB, Timestamp = ts, NumberOfPeople = 2, Type = GateEventType.Enter, CreatedAtUtc = createdAt });
        await db.SaveChangesAsync();

        var service = new AnalyticsQueryService(db);
        var results = await service.GetSummaryAsync(new SensorEventQuery { Gate = StadiumGate.GateA });

        Assert.All(results, r => Assert.Equal("Gate A", r.Gate));
        Assert.Single(results.Where(r => r.Type == "enter"));
    }

    [Fact]
    public async Task GetSummaryAsync_NoData_ReturnsEmptyList()
    {
        await using var db = CreateContext(_connection);
        await db.Database.EnsureCreatedAsync();

        var service = new AnalyticsQueryService(db);
        var results = await service.GetSummaryAsync(new SensorEventQuery());

        Assert.Empty(results);
    }

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
    }
}
