using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHttpClient("backend", client =>
{
    var baseUrl = Env.GetString("BACKEND_BASE_URL", "http://backend:8000");
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddHostedService<TelemetryCollectorWorker>();

await builder.Build().RunAsync();

public static class Env
{
    public static string GetString(string name, string defaultValue)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
    }

    public static int GetInt(string name, int defaultValue)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return int.TryParse(value, out var result) ? result : defaultValue;
    }

    public static double GetDouble(string name, double defaultValue)
    {
        var value = Environment.GetEnvironmentVariable(name);

        return double.TryParse(
            value,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out var result)
            ? result
            : defaultValue;
    }
}

public sealed class TelemetryCollectorWorker : BackgroundService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TelemetryCollectorWorker> _logger;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly string _machineId;
    private readonly string _toolId;
    private readonly string _mode;
    private readonly double _toolDiameterMm;
    private readonly int _periodMs;

    private readonly int _normalFrames;
    private readonly int _warningFrames;
    private readonly int _criticalFrames;

    private int _frameIndex;

    public TelemetryCollectorWorker(
        IHttpClientFactory httpClientFactory,
        ILogger<TelemetryCollectorWorker> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;

        _machineId = Env.GetString("MACHINE_ID", "HAAS_VF2_NGC_01");
        _toolId = Env.GetString("TOOL_ID", "T12");
        _mode = Env.GetString("COLLECTOR_MODE", "lifecycle").ToLowerInvariant();

        _toolDiameterMm = Env.GetDouble("TOOL_DIAMETER_MM", 10.0);
        _periodMs = Env.GetInt("COLLECTOR_PERIOD_MS", 500);

        _normalFrames = Env.GetInt("NORMAL_FRAMES", 70);
        _warningFrames = Env.GetInt("WARNING_FRAMES", 90);
        _criticalFrames = Env.GetInt("CRITICAL_FRAMES", 110);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Collector started. machineId={MachineId}, toolId={ToolId}, mode={Mode}, periodMs={PeriodMs}",
            _machineId,
            _toolId,
            _mode,
            _periodMs
        );

        var client = _httpClientFactory.CreateClient("backend");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var frame = BuildFrame();

                using var response = await client.PostAsJsonAsync(
                    "/api/telemetry",
                    frame,
                    _jsonOptions,
                    stoppingToken
                );

                var body = await response.Content.ReadAsStringAsync(stoppingToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Telemetry send failed. Status={StatusCode}, Body={Body}",
                        (int)response.StatusCode,
                        body
                    );
                }
                else if (_frameIndex % 10 == 0)
                {
                    _logger.LogInformation(
                        "Telemetry sent. frame={FrameIndex}, program={Program}, rpm={Rpm}, current={Current:F2}, power={Power:F2}",
                        _frameIndex,
                        frame.Program,
                        frame.SpindleRpm,
                        frame.SpindleCurrentA,
                        frame.SpindlePowerKw
                    );
                }

                _frameIndex++;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Collector iteration failed");
            }

            await Task.Delay(_periodMs, stoppingToken);
        }
    }

    private TelemetryFrame BuildFrame()
    {
        var stage = ResolveStage();

        return stage switch
        {
            CollectorStage.Normal => BuildNormalFrame(),
            CollectorStage.Warning => BuildWarningFrame(),
            CollectorStage.Critical => BuildCriticalFrame(),
            _ => BuildNormalFrame()
        };
    }

    private CollectorStage ResolveStage()
    {
        if (_mode == "normal")
            return CollectorStage.Normal;

        if (_mode == "warning")
            return CollectorStage.Warning;

        if (_mode == "critical")
            return CollectorStage.Critical;

        var total = _normalFrames + _warningFrames + _criticalFrames;
        var pos = total <= 0 ? 0 : _frameIndex % total;

        if (pos < _normalFrames)
            return CollectorStage.Normal;

        if (pos < _normalFrames + _warningFrames)
            return CollectorStage.Warning;

        return CollectorStage.Critical;
    }

    private TelemetryFrame BuildNormalFrame()
    {
        var rpm = 8200 + Random.Shared.Next(-20, 21);
        var current = 10.0 + Noise(0.10);
        var power = 2.1 + Noise(0.10);

        return BuildFrameCore(
            program: "OP10_COLLECTOR_NORMAL",
            rpm: rpm,
            currentA: current,
            powerKw: power
        );
    }

    private TelemetryFrame BuildWarningFrame()
    {
        var localIndex = Math.Max(0, (_frameIndex - _normalFrames) % Math.Max(1, _warningFrames));
        var wear = localIndex / Math.Max(1.0, _warningFrames - 1.0);

        var rpm = 8200 + Random.Shared.Next(-40, 41);
        var current = 17.5 + 4.5 * wear + Noise(0.20);
        var power = 5.0 + 1.6 * wear + Noise(0.15);

        return BuildFrameCore(
            program: "OP10_COLLECTOR_WARNING",
            rpm: rpm,
            currentA: current,
            powerKw: power
        );
    }

    private TelemetryFrame BuildCriticalFrame()
    {
        var localIndex = Math.Max(0, (_frameIndex - _normalFrames - _warningFrames) % Math.Max(1, _criticalFrames));
        var wear = localIndex / Math.Max(1.0, _criticalFrames - 1.0);

        var rpm = 8200 + Random.Shared.Next(-80, 81);
        var current = 22.0 + 4.0 * wear + Noise(0.30);
        var power = 7.0 + 1.5 * wear + Noise(0.20);

        return BuildFrameCore(
            program: "OP10_COLLECTOR_CRITICAL",
            rpm: rpm,
            currentA: current,
            powerKw: power
        );
    }

    private TelemetryFrame BuildFrameCore(
        string program,
        int rpm,
        double currentA,
        double powerKw)
    {
        return new TelemetryFrame
        {
            Ts = DateTimeOffset.UtcNow,
            MachineId = _machineId,
            ToolId = _toolId,
            SpindleRpm = rpm,
            SpindleCurrentA = (float)Math.Max(0.0, currentA),
            SpindlePowerKw = (float)Math.Max(0.0, powerKw),
            FeedMmMin = 1200,
            Program = program,
            CutFlag = true,
            ToolDiameterMm = (float)_toolDiameterMm,
            SpindleTorqueNm = null
        };
    }

    private static double Noise(double amplitude)
    {
        return (Random.Shared.NextDouble() * 2.0 - 1.0) * amplitude;
    }

    private enum CollectorStage
    {
        Normal,
        Warning,
        Critical
    }
}

public sealed class TelemetryFrame
{
    public DateTimeOffset Ts { get; init; }

    public string MachineId { get; init; } = "";
    public string ToolId { get; init; } = "";

    public int SpindleRpm { get; init; }
    public float SpindleCurrentA { get; init; }
    public float SpindlePowerKw { get; init; }
    public int FeedMmMin { get; init; }

    public string? Program { get; init; }
    public bool CutFlag { get; init; }

    public float? ToolDiameterMm { get; init; }
    public float? SpindleTorqueNm { get; init; }
}