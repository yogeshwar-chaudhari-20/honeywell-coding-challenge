using Microsoft.EntityFrameworkCore;
using StadiumAnalytics.Core.Dtos;
using StadiumAnalytics.Core.Models;
using StadiumAnalytics.Core.Services;
using StadiumAnalytics.Infrastructure.Data;

namespace StadiumAnalytics.Infrastructure.Services;

public sealed class AnalyticsQueryService : IAnalyticsQueryService
{
    private readonly StadiumDbContext _dbContext;

    public AnalyticsQueryService(StadiumDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public ParseSummaryQueryResult TryParseSummaryQuery(
        string? gate,
        string? type,
        DateTimeOffset? startTime,
        DateTimeOffset? endTime)
    {
        var errors = new List<(string Key, string Message)>();
        StadiumGate? parsedGate = null;
        GateEventType? parsedType = null;

        if (gate is not null)
        {
            if (gate.Length > 100)
            {
                errors.Add((nameof(gate), "Gate name must not exceed 100 characters."));
            }
            else if (!StadiumGateExtensions.TryParseDisplayName(gate, out var g))
            {
                var validGates = string.Join(", ", Enum.GetValues<StadiumGate>().Select(v => v.ToDisplayName()));
                errors.Add((nameof(gate), $"Invalid gate. Valid values: {validGates}"));
            }
            else
            {
                parsedGate = g;
            }
        }

        if (type is not null)
        {
            if (!Enum.TryParse<GateEventType>(type, ignoreCase: true, out var t))
            {
                errors.Add((nameof(type), "Invalid type. Valid values: enter, leave"));
            }
            else
            {
                parsedType = t;
            }
        }

        var query = new SensorEventQuery
        {
            Gate = parsedGate,
            Type = parsedType,
            StartTime = startTime,
            EndTime = endTime
        };

        foreach (var validationError in query.Validate())
            errors.Add((string.Empty, validationError));

        if (errors.Count > 0)
            return new ParseSummaryQueryResult(null, errors);

        return new ParseSummaryQueryResult(query, Array.Empty<(string, string)>());
    }

    public async Task<IReadOnlyList<AnalyticsSummaryItem>> GetSummaryAsync(
        SensorEventQuery query,
        CancellationToken cancellationToken = default)
    {
        var dbQuery = _dbContext.GateSensorEvents.AsQueryable();

        if (query.Gate is not null)
            dbQuery = dbQuery.Where(e => e.Gate == query.Gate);

        if (query.Type is not null)
            dbQuery = dbQuery.Where(e => e.Type == query.Type);

        if (query.StartTime is not null)
        {
            var startStr = query.StartTime.Value.ToUniversalTime().ToString("o");
            dbQuery = dbQuery.Where(e => string.Compare(e.Timestamp, startStr) >= 0);
        }

        if (query.EndTime is not null)
        {
            var endStr = query.EndTime.Value.ToUniversalTime().ToString("o");
            dbQuery = dbQuery.Where(e => string.Compare(e.Timestamp, endStr) <= 0);
        }

        var results = await dbQuery
            .GroupBy(e => new { e.Gate, e.Type })
            .Select(g => new
            {
                g.Key.Gate,
                g.Key.Type,
                NumberOfPeople = g.Sum(e => (long)e.NumberOfPeople)
            })
            .OrderBy(r => r.Gate)
            .ThenBy(r => r.Type)
            .ToListAsync(cancellationToken);

        return results.Select(r => new AnalyticsSummaryItem
        {
            Gate = r.Gate.ToDisplayName(),
            Type = r.Type.ToString().ToLowerInvariant(),
            NumberOfPeople = r.NumberOfPeople
        }).ToList();
    }
}
