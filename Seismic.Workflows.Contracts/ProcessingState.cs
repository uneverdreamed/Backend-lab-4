namespace Seismic.Workflows.Contracts;

// Ответ на query "GetState": что сейчас происходит с процессом.
// Query только читает состояние, в историю событий он не пишется.
public sealed class ProcessingState
{
    public long EarthquakeId { get; set; }
    public string RequestedBy { get; set; } = string.Empty;

    public string Stage { get; set; } = ProcessingStages.Started;
    public string? Severity { get; set; }

    // Данные, собранные параллельными activities
    public int StationsReported { get; set; }
    public double MaxAmplitude { get; set; }
    public int MacroReports { get; set; }
    public int MaxIntensity { get; set; }

    // До какого момента ждём решения сейсмолога (только для сильных событий)
    public DateTimeOffset? ConfirmationDeadline { get; set; }
    public string? Confirmation { get; set; }

    public string? Outcome { get; set; }
    public int NotifiedSubscribers { get; set; }
    public string? BulletinId { get; set; }

    public List<StageEntry> Timeline { get; set; } = new();
}

public sealed class StageEntry
{
    public string Stage { get; set; } = string.Empty;
    public DateTimeOffset At { get; set; }
}
