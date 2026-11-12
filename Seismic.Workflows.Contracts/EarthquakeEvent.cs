namespace Seismic.Workflows.Contracts;
public sealed class EarthquakeEvent
{
    public long EarthquakeId { get; init; }
    public string Region { get; init; } = string.Empty;
    public double Magnitude { get; init; }
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public DateTimeOffset OccurredAt { get; init; }
    public string RequestedBy { get; init; } = string.Empty;

    public ProcessingOptions Options { get; init; } = new();
}
public sealed class ProcessingOptions
{
    public int ConfirmationTimeoutSeconds { get; init; } = 60;
    public int FailFirstAttempts { get; init; }
}
