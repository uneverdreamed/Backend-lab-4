namespace Seismic.Workflows.Contracts;
public sealed class ConfirmationDecision
{
    public bool Approved { get; init; }
    public string Seismologist { get; init; } = string.Empty;
    public string? Comment { get; init; }
}
