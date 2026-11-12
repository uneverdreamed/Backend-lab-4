using Seismic.Workflows.Contracts;

namespace Seismic.Workflows.Api.Earthquakes.Contract;

public sealed class ProcessingStatusResponse
{
    public long EarthquakeId { get; init; }
    public string WorkflowId { get; init; } = string.Empty;

    public string WorkflowStatus { get; init; } = string.Empty;
    public ProcessingState? State { get; init; }
    public ProcessingResult? Result { get; init; }

    public string? Note { get; init; }
    public string TemporalUiUrl { get; init; } = string.Empty;
    public string ProcessedBy { get; init; } = string.Empty;
}
