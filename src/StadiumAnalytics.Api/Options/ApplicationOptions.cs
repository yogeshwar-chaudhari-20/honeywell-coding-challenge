namespace StadiumAnalytics.Api.Options;

public sealed class ApplicationOptions
{
    public const string SectionName = "Application";

    /// <summary>
    /// Optional base path when hosting behind a reverse proxy (e.g. "/stadium-analytics").
    /// Leave empty when not using a path prefix.
    /// </summary>
    public string PathBase { get; set; } = string.Empty;
}
