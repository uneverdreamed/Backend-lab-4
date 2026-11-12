namespace Seismic.Workflows.Contracts;
public sealed class ProcessingResult
{
    public long EarthquakeId { get; init; }
    public string RequestedBy { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;

    // published / published-unconfirmed / rejected
    public string Outcome { get; init; } = string.Empty;

    // not-required / confirmed / timeout-escalated
    public string Confirmation { get; init; } = string.Empty;

    public string? DecidedBy { get; init; }
    public string? Comment { get; init; }
    public bool Escalated { get; init; }

    public int NotifiedSubscribers { get; init; }
    public string? BulletinId { get; init; }

    public DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset FinishedAt { get; init; }
}
