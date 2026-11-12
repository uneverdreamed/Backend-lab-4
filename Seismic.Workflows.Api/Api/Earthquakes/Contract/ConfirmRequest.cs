using System.ComponentModel.DataAnnotations;

namespace Seismic.Workflows.Api.Earthquakes.Contract;

public sealed class ConfirmRequest
{
    [Required]
    [RegularExpression("^(approve|reject)$", ErrorMessage = "Допустимые значения: approve, reject")]
    public string Decision { get; init; } = "approve";

    [Required(AllowEmptyStrings = false)]
    [StringLength(100, MinimumLength = 2)]
    public string Seismologist { get; init; } = string.Empty;

    [StringLength(500)]
    public string? Comment { get; init; }
}
