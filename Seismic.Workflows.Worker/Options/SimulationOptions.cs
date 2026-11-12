namespace Seismic.Workflows.Worker.Options;

public sealed class SimulationOptions
{
    public int ActivityDelaySeconds { get; init; } = 3;
    public int NotifyDurationSeconds { get; init; } = 6;
    public double FailureProbability { get; init; } = 0.4;
}
