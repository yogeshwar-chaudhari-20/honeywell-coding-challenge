namespace StadiumAnalytics.Core.Dtos;

public sealed class AnalyticsSummaryItem
{
    public required string Gate { get; init; }
    public required string Type { get; init; }
    public required long NumberOfPeople { get; init; }
}
