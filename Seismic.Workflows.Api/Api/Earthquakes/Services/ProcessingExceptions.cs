namespace Seismic.Workflows.Api.Earthquakes.Services;
public sealed class ProcessingNotFoundException(long earthquakeId)
    : Exception($"Процесс обработки землетрясения {earthquakeId} не найден");

public sealed class ProcessingConflictException(string message) : Exception(message);

public sealed class TemporalUnavailableException(string message) : Exception(message);
