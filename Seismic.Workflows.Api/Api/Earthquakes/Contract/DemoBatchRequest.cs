using System.ComponentModel.DataAnnotations;

namespace Seismic.Workflows.Api.Earthquakes.Contract;

public sealed class DemoBatchRequest
{
    [Range(1, 30)]
    public int Count { get; init; } = 6;

    [Range(5, 86400)]
    public int ConfirmationTimeoutSeconds { get; init; } = 20;
}
