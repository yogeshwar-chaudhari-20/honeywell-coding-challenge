using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StadiumAnalytics.Core.Dtos;
using StadiumAnalytics.Core.Events;
using StadiumAnalytics.Core.Models;
using StadiumAnalytics.Infrastructure.Data;
using StadiumAnalytics.Infrastructure.Simulation;

namespace StadiumAnalytics.Tests;

public class AnalyticsIntegrationTests : IClassFixture<AnalyticsIntegrationTests.CustomFactory>, IDisposable
{
    private readonly CustomFactory _factory;
    private readonly HttpClient _client;

    public AnalyticsIntegrationTests(CustomFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetSummary_WithSeedData_ReturnsGroupedResults()
    {
        var response = await _client.GetAsync("/api/v1/analytics/summary");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var results = await response.Content.ReadFromJsonAsync<List<AnalyticsSummaryItem>>();
        Assert.NotNull(results);
        Assert.NotEmpty(results);

        foreach (var item in results)
        {
            Assert.True(item.NumberOfPeople > 0);
            Assert.Contains(item.Type, new[] { "enter", "leave" });
        }
    }

    [Fact]
    public async Task GetSummary_FilterByGate_ReturnsOnlyThatGate()
    {
        var response = await _client.GetAsync("/api/v1/analytics/summary?gate=Gate%20A");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var results = await response.Content.ReadFromJsonAsync<List<AnalyticsSummaryItem>>();
        Assert.NotNull(results);
        Assert.All(results, r => Assert.Equal("Gate A", r.Gate));
    }

    [Fact]
    public async Task GetSummary_FilterByType_ReturnsOnlyThatType()
    {
        var response = await _client.GetAsync("/api/v1/analytics/summary?type=enter");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var results = await response.Content.ReadFromJsonAsync<List<AnalyticsSummaryItem>>();
        Assert.NotNull(results);
        Assert.All(results, r => Assert.Equal("enter", r.Type));
    }

    [Fact]
    public async Task GetSummary_FilterByGateAndType_ReturnsFiltered()
    {
        var response = await _client.GetAsync("/api/v1/analytics/summary?gate=Gate%20B&type=leave");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var results = await response.Content.ReadFromJsonAsync<List<AnalyticsSummaryItem>>();
        Assert.NotNull(results);
        Assert.All(results, r =>
        {
            Assert.Equal("Gate B", r.Gate);
            Assert.Equal("leave", r.Type);
        });
    }

    [Fact]
    public async Task GetSummary_InvalidType_Returns400()
    {
        var response = await _client.GetAsync("/api/v1/analytics/summary?type=jump");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetSummary_InvalidGate_Returns400()
    {
        var response = await _client.GetAsync("/api/v1/analytics/summary?gate=Gate%20Z");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetSummary_StartTimeAfterEndTime_Returns400()
    {
        var future = Uri.EscapeDataString(DateTimeOffset.UtcNow.AddHours(1).ToString("o"));
        var past = Uri.EscapeDataString(DateTimeOffset.UtcNow.AddHours(-1).ToString("o"));

        var response = await _client.GetAsync($"/api/v1/analytics/summary?startTime={future}&endTime={past}");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetSummary_WithTimeRange_FiltersResults()
    {
        var startTime = Uri.EscapeDataString(DateTimeOffset.UtcNow.AddMinutes(-3).ToString("o"));
        var endTime = Uri.EscapeDataString(DateTimeOffset.UtcNow.ToString("o"));

        var response = await _client.GetAsync($"/api/v1/analytics/summary?startTime={startTime}&endTime={endTime}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var results = await response.Content.ReadFromJsonAsync<List<AnalyticsSummaryItem>>();
        Assert.NotNull(results);
    }

    [Fact]
    public async Task GetSummary_NoMatchingData_ReturnsEmptyList()
    {
        var farFuture = Uri.EscapeDataString(DateTimeOffset.UtcNow.AddYears(10).ToString("o"));
        var farFutureEnd = Uri.EscapeDataString(DateTimeOffset.UtcNow.AddYears(11).ToString("o"));

        var response = await _client.GetAsync($"/api/v1/analytics/summary?startTime={farFuture}&endTime={farFutureEnd}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var results = await response.Content.ReadFromJsonAsync<List<AnalyticsSummaryItem>>();
        Assert.NotNull(results);
        Assert.Empty(results);
    }

    [Fact]
    public async Task PublishDuplicate_RecordedInFailedEvents()
    {
        var channel = _factory.Services.GetRequiredService<IGateEventChannel>();
        var timestamp = new DateTimeOffset(2099, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var evt = new GateSensorEvent(StadiumGate.GateA, timestamp, 25, GateEventType.Enter);

        await channel.PublishAsync(evt);
        await Task.Delay(1000);

        await channel.PublishAsync(evt);
        await Task.Delay(1000);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StadiumDbContext>();

        var timestampStr = timestamp.ToUniversalTime().ToString("o");
        var mainCount = await db.GateSensorEvents.CountAsync(
            e => e.Gate == StadiumGate.GateA && e.Timestamp == timestampStr && e.Type == GateEventType.Enter);
        Assert.Equal(1, mainCount);

        var failedCount = await db.FailedEvents.CountAsync(
            e => e.Reason == "Duplicate" && e.Gate == "Gate A");
        Assert.True(failedCount >= 1);
    }

    [Fact]
    public async Task HealthCheckLive_Returns200()
    {
        var response = await _client.GetAsync("/health/live");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task HealthCheckReady_Returns200()
    {
        var response = await _client.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    public void Dispose()
    {
        _client.Dispose();
    }

    public class CustomFactory : WebApplicationFactory<Program>
    {
        private SqliteConnection? _connection;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<StadiumDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                _connection = new SqliteConnection("Data Source=:memory:");
                _connection.Open();

                services.AddDbContext<StadiumDbContext>(options =>
                    options.UseSqlite(_connection));

                services.Configure<EventSimulationOptions>(opts => opts.Enabled = false);
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                _connection?.Close();
                _connection?.Dispose();
            }
        }
    }
}
