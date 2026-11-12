namespace Seismic.Workflows.Api.Earthquakes.Contract;

public sealed class StartProcessingResponse
{
    public long EarthquakeId { get; init; }
    public string WorkflowId { get; init; } = string.Empty;
    public string? RunId { get; init; }
    public string Status { get; init; } = "accepted";
    public string StatusUrl { get; init; } = string.Empty;
    public string TemporalUiUrl { get; init; } = string.Empty;

    public string ProcessedBy { get; init; } = string.Empty;
}
