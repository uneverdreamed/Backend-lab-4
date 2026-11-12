namespace Seismic.Workflows.Worker.Options;

// пороги классификации события по магнитуде
public sealed class ClassificationOptions
{
    public double StrongThreshold { get; init; } = 6.0;
    public double ModerateThreshold { get; init; } = 4.5;
}
