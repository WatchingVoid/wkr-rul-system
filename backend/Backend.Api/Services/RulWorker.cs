using Backend.Api.Models;

namespace Backend.Api.Services;

public sealed class RulWorker : BackgroundService
{
    private readonly TelemetryRepository _telemetry;
    private readonly RulRepository _rul;
    private readonly MlClient _ml;
    private readonly IConfiguration _cfg;
    private readonly ILogger<RulWorker> _log;

    public RulWorker(
        TelemetryRepository telemetry,
        RulRepository rul,
        MlClient ml,
        IConfiguration cfg,
        ILogger<RulWorker> log)
    {
        _telemetry = telemetry;
        _rul = rul;
        _ml = ml;
        _cfg = cfg;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var windowSize = _cfg.GetValue<int>("Rul:WindowSize", 50);
        var periodSec = _cfg.GetValue<int>("Rul:PeriodSeconds", 5);
        var minWindowSize = Math.Max(10, windowSize / 3);
        var modelVersion = _cfg["Ml:ModelVersion"] ?? "external";

        _log.LogInformation("RulWorker started: windowSize={WindowSize}, period={PeriodSec}s",
            windowSize, periodSec);

        var timer = new PeriodicTimer(TimeSpan.FromSeconds(periodSec));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                // 1) Находим активные пары станок/инструмент
                var pairs = await _telemetry.GetActivePairsAsync(stoppingToken);
                if (pairs.Count == 0)
                {
                    _log.LogDebug("No active machine/tool pairs");
                    continue;
                }

                var stored = 0;

                foreach (var pair in pairs)
                {
                    // 2) Берём окно резания
                    var rows = await _telemetry.GetCutWindowAsync(pair.MachineId, pair.ToolId, windowSize, stoppingToken);
                    if (rows.Count < minWindowSize)
                        continue;

                    // 3) Готовим запрос ML (пока power/current/rpm)
                    var req = new MlPredictRequest
                    {
                        Power = rows.Select(r => r.SpindlePowerKw).ToList(),
                        Current = rows.Select(r => r.SpindleCurrentA).ToList(),
                        Rpm = rows.Select(r => (float)r.SpindleRpm).ToList()
                    };

                    var pred = await _ml.PredictAsync(req, stoppingToken);
                    if (pred is null) continue;

                    // 4) Пишем прогноз в БД через SQL-функцию
                    await _rul.InsertPredictionAsync(
                        ts: DateTimeOffset.UtcNow,
                        machineId: pair.MachineId,
                        toolId: pair.ToolId,
                        rulMinutes: pred.Rul_Minutes,
                        alarmLevel: pred.Alarm_Level,
                        modelVersion: modelVersion,
                        ct: stoppingToken);

                    stored++;
                }

                if (stored > 0)
                    _log.LogInformation("RulWorker cycle completed: {Stored} predictions stored", stored);
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "RulWorker iteration failed");
            }
        }
    }
}