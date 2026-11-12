using System.ComponentModel.DataAnnotations;

namespace Seismic.Workflows.Api.Earthquakes.Contract;

public sealed class StartProcessingRequest
{
    [Range(1, long.MaxValue)]
    public long EarthquakeId { get; init; }

    [Required(AllowEmptyStrings = false)]
    [StringLength(50, MinimumLength = 2)]
    public string Region { get; init; } = string.Empty;

    [Range(0.0, 10.0)]
    public double Magnitude { get; init; }

    [Range(-90.0, 90.0)]
    public double Latitude { get; init; }

    [Range(-180.0, 180.0)]
    public double Longitude { get; init; }

    public DateTimeOffset OccurredAt { get; init; }

    [Range(5, 86400)]
    public int ConfirmationTimeoutSeconds { get; init; } = 60;

    [Range(0, 5)]
    public int FailFirstAttempts { get; init; }
}
