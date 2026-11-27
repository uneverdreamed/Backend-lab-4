using Seismic.Workflows.Api.Earthquakes.Contract;

namespace Seismic.Workflows.Api.Earthquakes.Services;

public interface IEarthquakeProcessingService
{
    Task<StartProcessingResponse> StartAsync(StartProcessingRequest request);
    Task<IReadOnlyList<StartProcessingResponse>> StartDemoBatchAsync(DemoBatchRequest request);
    Task ConfirmAsync(long earthquakeId, ConfirmRequest request);
    Task<ProcessingStatusResponse> GetStatusAsync(long earthquakeId);
    Task<IReadOnlyList<ProcessListItem>> ListAsync(int limit);
}
