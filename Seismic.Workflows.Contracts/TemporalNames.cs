namespace Seismic.Workflows.Contracts;
public static class TemporalNames
{
    public const string TaskQueue = "seismic-processing";

    public const string WorkflowName = "EarthquakeProcessing";
    public const string ConfirmSignal = "confirm";
    public const string StateQuery = "GetState";
    public static string WorkflowId(long earthquakeId) => $"earthquake-{earthquakeId}";
}
