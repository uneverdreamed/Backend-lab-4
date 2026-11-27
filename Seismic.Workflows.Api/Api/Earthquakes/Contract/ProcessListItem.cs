namespace Seismic.Workflows.Api.Earthquakes.Contract;

public sealed class ProcessListItem
{
    public long EarthquakeId { get; init; }
    public string WorkflowId { get; init; } = string.Empty;

    // Running / Completed / Failed / TimedOut / Terminated / Canceled
    public string WorkflowStatus { get; init; } = string.Empty;

    public DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset? ClosedAt { get; init; }

    // Стадия процесса (только если процесс выполняется и Worker ответил на query)
    public string? Stage { get; init; }
    public string? Severity { get; init; }
    public string? Confirmation { get; init; }
    public DateTimeOffset? ConfirmationDeadline { get; init; }

    // Итог (только для завершённых процессов)
    public string? Outcome { get; init; }
    public int NotifiedSubscribers { get; init; }
    public string? BulletinId { get; init; }

    public string? Note { get; init; }
    public string TemporalUiUrl { get; init; } = string.Empty;
}
