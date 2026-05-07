using Backend.Api.Models;

namespace Backend.Api.Services;

public sealed class RulWorker : BackgroundService
{
    private readonly TelemetryRepository _telemetry;
    private readonly RulRepository _rul;
    private readonly AlarmRepository _alarm;
    private readonly FeatureExtractor _features;
    private readonly MlClient _ml;
    private readonly IConfiguration _cfg;
    private readonly ILogger<RulWorker> _log;

    public RulWorker(
        TelemetryRepository telemetry,
        RulRepository rul,
        AlarmRepository alarm,
        FeatureExtractor features,
        MlClient ml,
        IConfiguration cfg,
        ILogger<RulWorker> log)
    {
        _telemetry = telemetry;
        _rul = rul;
        _alarm = alarm;
        _features = features;
        _ml = ml;
        _cfg = cfg;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var windowSize = _cfg.GetValue<int>("Rul:WindowSize", 50);
        var periodSec = _cfg.GetValue<int>("Rul:PeriodSeconds", 5);
        var minWindowSize = Math.Max(10, windowSize / 3);

        _log.LogInformation(
            "RulWorker started: windowSize={WindowSize}, period={PeriodSec}s",
            windowSize,
            periodSec
        );

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(periodSec));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                var pairs = await _telemetry.GetActivePairsAsync(stoppingToken);

                if (pairs.Count == 0)
                {
                    _log.LogDebug("No active machine/tool pairs");
                    continue;
                }

                var stored = 0;

                foreach (var pair in pairs)
                {
                    var rows = await _telemetry.GetCutWindowAsync(
                        pair.MachineId,
                        pair.ToolId,
                        windowSize,
                        stoppingToken
                    );

                    if (rows.Count < minWindowSize)
                    {
                        _log.LogDebug(
                            "Not enough rows for {MachineId}/{ToolId}: {Count}/{Min}",
                            pair.MachineId,
                            pair.ToolId,
                            rows.Count,
                            minWindowSize
                        );

                        continue;
                    }

                    var featureDict = _features.ExtractFromWindow(rows);

                    var mlReq = new MlPredictRequest
                    {
                        Features = featureDict
                    };

                    var pred = await _ml.PredictAsync(mlReq, stoppingToken);

                    if (pred is null)
                    {
                        _log.LogWarning(
                            "ML returned null prediction for {MachineId}/{ToolId}",
                            pair.MachineId,
                            pair.ToolId
                        );

                        continue;
                    }

                    var now = DateTimeOffset.UtcNow;

                    await _rul.InsertPredictionAsync(
                        ts: now,
                        machineId: pair.MachineId,
                        toolId: pair.ToolId,
                        rulMinutes: pred.RulMinutes,
                        alarmLevel: pred.AlarmLevel,
                        alarmCode: pred.AlarmCode,
                        state: pred.State,
                        message: pred.Message,
                        requiredAction: pred.RequiredAction,
                        modelVersion: pred.ModelVersion,
                        features: pred.UsedFeatures.Count > 0 ? pred.UsedFeatures : featureDict,
                        explanation: pred.Explanation,
                        ct: stoppingToken
                    );

                    if (pred.AlarmLevel > 0)
                    {
                        await _alarm.InsertAlarmAsync(
                            ts: now,
                            machineId: pair.MachineId,
                            toolId: pair.ToolId,
                            rulMinutes: pred.RulMinutes,
                            alarmLevel: pred.AlarmLevel,
                            alarmCode: pred.AlarmCode,
                            alarmMessage: pred.Message,
                            requiredAction: pred.RequiredAction,
                            modelVersion: pred.ModelVersion,
                            ct: stoppingToken
                        );

                        _log.LogWarning(
                            "ALARM {AlarmCode}: {MachineId}/{ToolId}, RUL={RulMinutes:F1}, action={RequiredAction}",
                            pred.AlarmCode,
                            pair.MachineId,
                            pair.ToolId,
                            pred.RulMinutes,
                            pred.RequiredAction
                        );
                    }

                    stored++;
                }

                if (stored > 0)
                {
                    _log.LogInformation(
                        "RulWorker cycle completed: {Stored} predictions stored",
                        stored
                    );
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "RulWorker iteration failed");
            }
        }
    }
}