using Microsoft.AspNetCore.Mvc;
using Seismic.Workflows.Api.Common;
using Seismic.Workflows.Api.Earthquakes.Contract;
using Seismic.Workflows.Contracts;
using Temporalio.Api.Enums.V1;
using Temporalio.Client;
using Temporalio.Exceptions;

namespace Seismic.Workflows.Api.Earthquakes.Services;

public sealed class EarthquakeProcessingService : IEarthquakeProcessingService
{
    private static readonly (string Region, double Lat, double Lon)[] DemoRegions =
    [
        ("kamchatka", 52.2, 158.6),
        ("caucasus", 43.5, 42.7),
        ("altai", 50.0, 87.0)
    ];

    private readonly ITemporalClient _client;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EarthquakeProcessingService> _logger;

    public EarthquakeProcessingService(
        ITemporalClient client, IConfiguration configuration, ILogger<EarthquakeProcessingService> logger)
    {
        _client = client;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<StartProcessingResponse> StartAsync(StartProcessingRequest request)
    {
        var input = new EarthquakeEvent
        {
            EarthquakeId = request.EarthquakeId,
            Region = request.Region.Trim().ToLowerInvariant(),
            Magnitude = request.Magnitude,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            OccurredAt = request.OccurredAt == default ? DateTimeOffset.UtcNow : request.OccurredAt,
            RequestedBy = MeInfo.Line,
            Options = new ProcessingOptions
            {
                ConfirmationTimeoutSeconds = request.ConfirmationTimeoutSeconds,
                FailFirstAttempts = request.FailFirstAttempts
            }
        };

        var workflowId = TemporalNames.WorkflowId(request.EarthquakeId);

        try
        {

            var handle = await _client.StartWorkflowAsync(
                TemporalNames.WorkflowName,
                [input],
                new WorkflowOptions(id: workflowId, taskQueue: TemporalNames.TaskQueue)
                {
                    IdReusePolicy = WorkflowIdReusePolicy.AllowDuplicateFailedOnly,
                    Memo = new Dictionary<string, object> { ["initiatedBy"] = MeInfo.Line }
                });

            _logger.LogInformation(
                "Earthquake {EarthquakeId} (M{Magnitude}, {Region}) accepted, workflow {WorkflowId} started",
                input.EarthquakeId, input.Magnitude, input.Region, handle.Id);

            return new StartProcessingResponse
            {
                EarthquakeId = input.EarthquakeId,
                WorkflowId = handle.Id,
                RunId = handle.ResultRunId,
                StatusUrl = $"/earthquakes/{input.EarthquakeId}",
                TemporalUiUrl = UiUrl(handle.Id),
                ProcessedBy = MeInfo.Line
            };
        }
        catch (WorkflowAlreadyStartedException)
        {
            throw new ProcessingConflictException(
                $"Обработка землетрясения {request.EarthquakeId} уже запущена или завершена (workflow {workflowId})");
        }
        catch (RpcException ex) when (ex.Code == RpcException.StatusCode.Unavailable)
        {
            throw new TemporalUnavailableException("Сервер Temporal недоступен");
        }
    }

    public async Task<IReadOnlyList<StartProcessingResponse>> StartDemoBatchAsync(DemoBatchRequest request)
    {
        var responses = new List<StartProcessingResponse>();

        for (var i = 0; i < request.Count; i++)
        {
            var (region, lat, lon) = DemoRegions[Random.Shared.Next(DemoRegions.Length)];

            var magnitude = Random.Shared.NextDouble() < 0.3
                ? Math.Round(6.0 + Random.Shared.NextDouble() * 1.5, 1)
                : Math.Round(3.0 + Random.Shared.NextDouble() * 2.9, 1);

            try
            {
                responses.Add(await StartAsync(new StartProcessingRequest
                {
                    EarthquakeId = Random.Shared.NextInt64(10_000, 99_999),
                    Region = region,
                    Magnitude = magnitude,
                    Latitude = lat,
                    Longitude = lon,
                    OccurredAt = DateTimeOffset.UtcNow,
                    ConfirmationTimeoutSeconds = request.ConfirmationTimeoutSeconds
                }));
            }
            catch (ProcessingConflictException)
            {
            }
        }

        return responses;
    }

    public async Task ConfirmAsync(long earthquakeId, ConfirmRequest request)
    {
        var handle = _client.GetWorkflowHandle(TemporalNames.WorkflowId(earthquakeId));

        try
        {
            var description = await handle.DescribeAsync();
            if (description.Status != WorkflowExecutionStatus.Running)
            {
                throw new ProcessingConflictException(
                    $"Обработка землетрясения {earthquakeId} уже завершена (статус {description.Status}), решение не принимается");
            }


            await handle.SignalAsync(TemporalNames.ConfirmSignal,
            [
                new ConfirmationDecision
                {
                    Approved = request.Decision == "approve",
                    Seismologist = request.Seismologist.Trim(),
                    Comment = request.Comment
                }
            ]);

            _logger.LogInformation("Earthquake {EarthquakeId}: decision {Decision} from {Seismologist} sent to workflow",
                earthquakeId, request.Decision, request.Seismologist);
        }
        catch (RpcException ex) when (ex.Code == RpcException.StatusCode.NotFound)
        {
            throw new ProcessingNotFoundException(earthquakeId);
        }
        catch (RpcException ex) when (ex.Code == RpcException.StatusCode.Unavailable)
        {
            throw new TemporalUnavailableException("Сервер Temporal недоступен");
        }
    }

    public async Task<ProcessingStatusResponse> GetStatusAsync(long earthquakeId)
    {
        var workflowId = TemporalNames.WorkflowId(earthquakeId);
        var handle = _client.GetWorkflowHandle(workflowId);

        try
        {
            var description = await handle.DescribeAsync();

            ProcessingState? state = null;
            ProcessingResult? result = null;
            string? note = null;

            if (description.Status == WorkflowExecutionStatus.Running)
            {
                try
                {
                    state = await handle.QueryAsync<ProcessingState>(TemporalNames.StateQuery, [], new WorkflowQueryOptions
                    {
                        Rpc = new RpcOptions { Timeout = TimeSpan.FromSeconds(3) }
                    });
                }
                catch (Exception)
                {
                    note = "Нет ответа на query: ни один Worker не запущен. Процесс не потерян, он продолжится после старта Worker'а.";
                }
            }
            else if (description.Status == WorkflowExecutionStatus.Completed)
            {
                result = await handle.GetResultAsync<ProcessingResult>();
            }
            else
            {
                note = $"Процесс завершился со статусом {description.Status}. Подробности в истории событий.";
            }

            return new ProcessingStatusResponse
            {
                EarthquakeId = earthquakeId,
                WorkflowId = workflowId,
                WorkflowStatus = description.Status.ToString(),
                State = state,
                Result = result,
                Note = note,
                TemporalUiUrl = UiUrl(workflowId),
                ProcessedBy = MeInfo.Line
            };
        }
        catch (RpcException ex) when (ex.Code == RpcException.StatusCode.NotFound)
        {
            throw new ProcessingNotFoundException(earthquakeId);
        }
        catch (RpcException ex) when (ex.Code == RpcException.StatusCode.Unavailable)
        {
            throw new TemporalUnavailableException("Сервер Temporal недоступен");
        }
    }

    private string UiUrl(string workflowId) =>
        $"{_configuration["Temporal:UiUrl"]}/namespaces/default/workflows/{workflowId}";
}
