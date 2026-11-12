namespace Seismic.Workflows.Contracts;

// сводка по инструментальным данным станций
public sealed class StationReadings
{
    public int StationsReported { get; init; }
    public double MaxAmplitude { get; init; }
}

// сводка по пользовательским отчётам (лаба 1)
public sealed class MacroseismicSummary
{
    public int ReportsCount { get; init; }
    public int MaxIntensity { get; init; }
}
