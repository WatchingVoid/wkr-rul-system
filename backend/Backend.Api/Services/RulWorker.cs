using Microsoft.EntityFrameworkCore;
using Backend.Api.Data;
using Backend.Api.Entities;
using Backend.Api.Models;

namespace Backend.Api.Services;

public sealed class RulWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly MlClient _ml;
    private readonly IConfiguration _cfg;
    private readonly ILogger<RulWorker> _log;

    public RulWorker(IServiceScopeFactory scopeFactory, MlClient ml, IConfiguration cfg, ILogger<RulWorker> log)
    {
        _scopeFactory = scopeFactory;
        _ml = ml;
        _cfg = cfg;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var windowSize = _cfg.GetValue<int>("Rul:WindowSize", 50);
        var periodSec = _cfg.GetValue<int>("Rul:PeriodSeconds", 5);

        _log.LogInformation("RulWorker started: windowSize={WindowSize}, period={PeriodSec}s", windowSize, periodSec);

        var timer = new PeriodicTimer(TimeSpan.FromSeconds(periodSec));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Берём последние windowSize точек резания (cut_flag = true)
                var rows = await db.TelemetrySpindle
                    .Where(x => x.CutFlag)
                    .OrderByDescending(x => x.Ts)
                    .Take(windowSize)
                    .ToListAsync(stoppingToken);

                if (rows.Count < Math.Max(10, windowSize / 3))
                {
                    _log.LogDebug("Not enough cut frames: {Count}", rows.Count);
                    continue;
                }

                // Важно: окно должно идти по времени вперёд
                rows.Reverse();
                var last = rows[^1];

                var req = new MlPredictRequest
                {
                    Power = rows.Select(r => r.SpindlePowerKw).ToList(),
                    Current = rows.Select(r => r.SpindleCurrentA).ToList(),
                    Rpm = rows.Select(r => (float)r.SpindleRpm).ToList(),
                };

                var pred = await _ml.PredictAsync(req, stoppingToken);
                if (pred is null) continue;

                db.RulPredictions.Add(new RulPredictionEntity
                {
                    Ts = DateTimeOffset.UtcNow,
                    MachineId = last.MachineId,
                    ToolId = last.ToolId,
                    RulMinutes = pred.Rul_Minutes,
                    AlarmLevel = pred.Alarm_Level,
                    ModelVersion = "dev"
                });

                await db.SaveChangesAsync(stoppingToken);

                _log.LogInformation("RUL={Rul} min, alarm={Alarm}", pred.Rul_Minutes, pred.Alarm_Level);
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "RulWorker iteration failed");
            }
        }
    }
}