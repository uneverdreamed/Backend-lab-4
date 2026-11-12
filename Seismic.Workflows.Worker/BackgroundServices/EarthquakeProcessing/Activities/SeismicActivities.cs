using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Seismic.Workflows.Contracts;
using Seismic.Workflows.Worker.Options;
using Temporalio.Activities;
using Temporalio.Exceptions;

namespace Seismic.Workflows.Worker.BackgroundServices.EarthquakeProcessing.Activities;

public class SeismicActivities(
    ILogger<SeismicActivities> logger,
    IOptions<SimulationOptions> simulation,
    IOptions<ClassificationOptions> classification)
{
    private static readonly (string Name, string Region, double MinMagnitude)[] Subscribers =
    [
        ("Петров А.И.", "kamchatka", 4.0),
        ("Сидорова М.В.", "kamchatka", 5.5),
        ("Иванов К.С.", "caucasus", 4.0),
        ("Козлова О.П.", "caucasus", 3.0),
        ("Волков Д.Е.", "altai", 4.5)
    ];

    private TimeSpan Delay => TimeSpan.FromSeconds(simulation.Value.ActivityDelaySeconds);

    // классификация по магнитуде
    [Activity]
    public async Task<string> ClassifyEventAsync(EarthquakeEvent input)
    {
        var context = ActivityExecutionContext.Current;
        logger.LogInformation("[classify] earthquake {Id} M{Magnitude} {Region}, attempt {Attempt}",
            input.EarthquakeId, input.Magnitude, input.Region, context.Info.Attempt);

        await Task.Delay(TimeSpan.FromSeconds(1), context.CancellationToken);
        if (input.Magnitude is < 0 or > 10 || string.IsNullOrWhiteSpace(input.Region))
        {
            throw new ApplicationFailureException(
                $"Некорректные данные события {input.EarthquakeId}",
                errorType: "InvalidEarthquakeData",
                nonRetryable: true);
        }

        var th = classification.Value;
        var severity = input.Magnitude >= th.StrongThreshold ? SeverityLevels.Strong
            : input.Magnitude >= th.ModerateThreshold ? SeverityLevels.Moderate
            : SeverityLevels.Light;

        logger.LogInformation("[classify] earthquake {Id} -> {Severity}", input.EarthquakeId, severity);
        return severity;
    }

    // сбор инструментальных данных со станций. иммитирует временной сбой
    [Activity]
    public async Task<StationReadings> CollectStationReadingsAsync(long earthquakeId, string region, int failFirstAttempts)
    {
        var context = ActivityExecutionContext.Current;
        var attempt = context.Info.Attempt;
        logger.LogInformation("[readings] earthquake {Id} ({Region}): collecting station data, attempt {Attempt}",
            earthquakeId, region, attempt);

        await Task.Delay(Delay, context.CancellationToken);

        if (attempt <= failFirstAttempts || Random.Shared.NextDouble() < simulation.Value.FailureProbability)
        {
            logger.LogWarning("[readings] earthquake {Id}: telemetry gateway unavailable, attempt {Attempt} failed", earthquakeId, attempt);
            throw new InvalidOperationException("Шлюз телеметрии станций временно недоступен");
        }

        var rnd = new Random(unchecked((int)earthquakeId));
        var result = new StationReadings
        {
            StationsReported = rnd.Next(3, 9),
            MaxAmplitude = Math.Round(1 + rnd.NextDouble() * 8, 2)
        };

        logger.LogInformation("[readings] earthquake {Id}: {Stations} stations, max amplitude {Amplitude}",
            earthquakeId, result.StationsReported, result.MaxAmplitude);
        return result;
    }

    // сводка пользовательских отчётов о макросейсмических проявлениях
    [Activity]
    public async Task<MacroseismicSummary> CollectMacroseismicReportsAsync(long earthquakeId, string region)
    {
        var context = ActivityExecutionContext.Current;
        logger.LogInformation("[reports] earthquake {Id} ({Region}): collecting user reports, attempt {Attempt}",
            earthquakeId, region, context.Info.Attempt);

        await Task.Delay(Delay, context.CancellationToken);

        var rnd = new Random(unchecked((int)earthquakeId) + 1);
        var result = new MacroseismicSummary
        {
            ReportsCount = rnd.Next(5, 120),
            MaxIntensity = rnd.Next(2, 9)
        };

        logger.LogInformation("[reports] earthquake {Id}: {Count} reports, max intensity {Intensity}",
            earthquakeId, result.ReportsCount, result.MaxIntensity);
        return result;
    }

    // сигнал не пришёл вовремя, дежурного заменяет старший сейсмолог
    [Activity]
    public async Task<string> EscalateToSeniorSeismologistAsync(long earthquakeId, int waitedSeconds)
    {
        var context = ActivityExecutionContext.Current;
        logger.LogWarning("[escalate] earthquake {Id}: no confirmation for {Seconds}s, escalating to senior seismologist",
            earthquakeId, waitedSeconds);

        await Task.Delay(TimeSpan.FromSeconds(1), context.CancellationToken);
        return "senior-seismologist (auto-escalation)";
    }

    // рассылка уведомлений подписчикам региона
    [Activity]
    public async Task<int> NotifySubscribersAsync(long earthquakeId, string region, double magnitude, string severity, bool preliminary)
    {
        var context = ActivityExecutionContext.Current;
        var matched = Subscribers
            .Where(s => s.Region == region && s.MinMagnitude <= magnitude)
            .ToList();

        logger.LogInformation("[notify] earthquake {Id} ({Severity}{Preliminary}): notifying {Count} subscriber(s) in {Region}, attempt {Attempt}",
            earthquakeId, severity, preliminary ? ", PRELIMINARY" : "", matched.Count, region, context.Info.Attempt);

        for (var second = 0; second < simulation.Value.NotifyDurationSeconds; second++)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), context.CancellationToken);
            context.Heartbeat();
        }

        foreach (var subscriber in matched)
        {
            logger.LogInformation("[notify] earthquake {Id}: sent to {Name}", earthquakeId, subscriber.Name);
        }

        return matched.Count;
    }

    // публикация бюллетеня
    [Activity]
    public async Task<string> PublishBulletinAsync(long earthquakeId, string severity, string confirmation, int notified)
    {
        var context = ActivityExecutionContext.Current;
        logger.LogInformation("[publish] earthquake {Id}: publishing bulletin ({Severity}, confirmation {Confirmation}, notified {Notified}), attempt {Attempt}",
            earthquakeId, severity, confirmation, notified, context.Info.Attempt);

        await Task.Delay(TimeSpan.FromSeconds(1), context.CancellationToken);

        var bulletinId = $"bulletin-{earthquakeId}-{severity}";
        logger.LogInformation("[publish] earthquake {Id}: published {Bulletin}", earthquakeId, bulletinId);
        return bulletinId;
    }
}
