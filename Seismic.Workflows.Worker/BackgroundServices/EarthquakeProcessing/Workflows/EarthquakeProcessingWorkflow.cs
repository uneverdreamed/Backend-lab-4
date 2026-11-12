using Microsoft.Extensions.Logging;
using Seismic.Workflows.Contracts;
using Seismic.Workflows.Worker.BackgroundServices.EarthquakeProcessing.Activities;
using Temporalio.Common;
using Temporalio.Workflows;

namespace Seismic.Workflows.Worker.BackgroundServices.EarthquakeProcessing.Workflows;
[Workflow(TemporalNames.WorkflowName)]
public class EarthquakeProcessingWorkflow
{
    private ConfirmationDecision? _decision;

    private readonly ProcessingState _state = new();
    [WorkflowQuery(TemporalNames.StateQuery)]
    public ProcessingState GetState() => _state;
    [WorkflowSignal(TemporalNames.ConfirmSignal)]
    public Task ConfirmAsync(ConfirmationDecision decision)
    {
        if (_decision is not null)
        {
            Workflow.Logger.LogInformation("Earthquake {Id}: decision already received, signal ignored", _state.EarthquakeId);
            return Task.CompletedTask;
        }

        _decision = decision;
        return Task.CompletedTask;
    }

    [WorkflowRun]
    public async Task<ProcessingResult> RunAsync(EarthquakeEvent input)
    {
        var options = input.Options ?? new ProcessingOptions();
        var confirmationTimeout = TimeSpan.FromSeconds(options.ConfirmationTimeoutSeconds);
        var startedAt = Workflow.UtcNow;

        _state.EarthquakeId = input.EarthquakeId;
        _state.RequestedBy = input.RequestedBy;

        var defaultOptions = new ActivityOptions
        {
            StartToCloseTimeout = TimeSpan.FromSeconds(30),
            RetryPolicy = new RetryPolicy
            {
                InitialInterval = TimeSpan.FromSeconds(1),
                BackoffCoefficient = 2,
                MaximumInterval = TimeSpan.FromSeconds(10),
                MaximumAttempts = 3
            }
        };

        var classifyOptions = new ActivityOptions
        {
            StartToCloseTimeout = TimeSpan.FromSeconds(10),
            RetryPolicy = new RetryPolicy
            {
                MaximumAttempts = 3,
                NonRetryableErrorTypes = new[] { "InvalidEarthquakeData" }
            }
        };

        var flakyOptions = new ActivityOptions
        {
            StartToCloseTimeout = TimeSpan.FromSeconds(20),
            RetryPolicy = new RetryPolicy
            {
                InitialInterval = TimeSpan.FromSeconds(2),
                BackoffCoefficient = 2,
                MaximumInterval = TimeSpan.FromSeconds(10),
                MaximumAttempts = 10
            }
        };

        var notifyOptions = new ActivityOptions
        {
            StartToCloseTimeout = TimeSpan.FromSeconds(60),
            HeartbeatTimeout = TimeSpan.FromSeconds(5),
            RetryPolicy = new RetryPolicy
            {
                InitialInterval = TimeSpan.FromSeconds(1),
                BackoffCoefficient = 2,
                MaximumAttempts = 5
            }
        };

        // ---- Этап 1 (последовательно): классификация ----
        SetStage(ProcessingStages.Classifying);
        var severity = await Workflow.ExecuteActivityAsync(
            (SeismicActivities a) => a.ClassifyEventAsync(input), classifyOptions);
        _state.Severity = severity;
        Workflow.Logger.LogInformation("Earthquake {Id}: M{Magnitude} classified as {Severity}", input.EarthquakeId, input.Magnitude, severity);

        // ---- Этап 2 (параллельно): инструментальные данные и пользовательские отчёты ----
        SetStage(ProcessingStages.CollectingData);
        var readingsTask = Workflow.ExecuteActivityAsync(
            (SeismicActivities a) => a.CollectStationReadingsAsync(input.EarthquakeId, input.Region, options.FailFirstAttempts), flakyOptions);
        var reportsTask = Workflow.ExecuteActivityAsync(
            (SeismicActivities a) => a.CollectMacroseismicReportsAsync(input.EarthquakeId, input.Region), defaultOptions);

        await Workflow.WhenAllAsync(readingsTask, reportsTask);

        var readings = await readingsTask;
        var reports = await reportsTask;
        _state.StationsReported = readings.StationsReported;
        _state.MaxAmplitude = readings.MaxAmplitude;
        _state.MacroReports = reports.ReportsCount;
        _state.MaxIntensity = reports.MaxIntensity;

        // ---- Этап 3 (ветвление): сильное событие требует подтверждения сейсмолога ----
        var confirmation = ConfirmationStatuses.NotRequired;
        string? decidedBy = null;
        string? comment = null;
        var escalated = false;

        if (severity == SeverityLevels.Strong)
        {
            SetStage(ProcessingStages.AwaitingConfirmation);
            _state.ConfirmationDeadline = Workflow.UtcNow + confirmationTimeout;
            Workflow.Logger.LogInformation(
                "Earthquake {Id}: waiting for seismologist decision ({Timeout}s)", input.EarthquakeId, options.ConfirmationTimeoutSeconds);

            var signalReceived = await Workflow.WaitConditionAsync(() => _decision != null, confirmationTimeout);

            if (!signalReceived)
            {
                Workflow.Logger.LogWarning("Earthquake {Id}: no decision in time, escalating", input.EarthquakeId);
                SetStage(ProcessingStages.Escalating);

                decidedBy = await Workflow.ExecuteActivityAsync(
                    (SeismicActivities a) => a.EscalateToSeniorSeismologistAsync(input.EarthquakeId, options.ConfirmationTimeoutSeconds), defaultOptions);
                escalated = true;
                confirmation = ConfirmationStatuses.TimeoutEscalated;
            }
            else if (!_decision!.Approved)
            {
                Workflow.Logger.LogInformation(
                    "Earthquake {Id}: rejected by {Seismologist} ({Comment})", input.EarthquakeId, _decision.Seismologist, _decision.Comment);

                SetStage(ProcessingStages.Rejected);
                _state.Confirmation = "rejected";
                _state.Outcome = ProcessingOutcomes.Rejected;

                return new ProcessingResult
                {
                    EarthquakeId = input.EarthquakeId,
                    RequestedBy = input.RequestedBy,
                    Severity = severity,
                    Outcome = ProcessingOutcomes.Rejected,
                    Confirmation = "rejected",
                    DecidedBy = _decision.Seismologist,
                    Comment = _decision.Comment,
                    StartedAt = startedAt,
                    FinishedAt = Workflow.UtcNow
                };
            }
            else
            {
                Workflow.Logger.LogInformation("Earthquake {Id}: confirmed by {Seismologist}", input.EarthquakeId, _decision.Seismologist);
                decidedBy = _decision.Seismologist;
                comment = _decision.Comment;
                confirmation = ConfirmationStatuses.Confirmed;
            }
        }

        _state.Confirmation = confirmation;
        var preliminary = confirmation == ConfirmationStatuses.TimeoutEscalated;

        // ---- Этап 4 (последовательно): рассылка и публикация бюллетеня ----
        SetStage(ProcessingStages.Notifying);
        var notified = await Workflow.ExecuteActivityAsync(
            (SeismicActivities a) => a.NotifySubscribersAsync(input.EarthquakeId, input.Region, input.Magnitude, severity, preliminary), notifyOptions);
        _state.NotifiedSubscribers = notified;

        SetStage(ProcessingStages.Publishing);
        var bulletinId = await Workflow.ExecuteActivityAsync(
            (SeismicActivities a) => a.PublishBulletinAsync(input.EarthquakeId, severity, confirmation, notified), defaultOptions);
        _state.BulletinId = bulletinId;

        var outcome = preliminary ? ProcessingOutcomes.PublishedUnconfirmed : ProcessingOutcomes.Published;
        _state.Outcome = outcome;
        SetStage(ProcessingStages.Completed);
        Workflow.Logger.LogInformation("Earthquake {Id}: {Outcome}, bulletin {Bulletin}", input.EarthquakeId, outcome, bulletinId);

        return new ProcessingResult
        {
            EarthquakeId = input.EarthquakeId,
            RequestedBy = input.RequestedBy,
            Severity = severity,
            Outcome = outcome,
            Confirmation = confirmation,
            DecidedBy = decidedBy,
            Comment = comment,
            Escalated = escalated,
            NotifiedSubscribers = notified,
            BulletinId = bulletinId,
            StartedAt = startedAt,
            FinishedAt = Workflow.UtcNow
        };
    }

    private void SetStage(string stage)
    {
        _state.Stage = stage;
        _state.Timeline.Add(new StageEntry { Stage = stage, At = Workflow.UtcNow });
    }
}
