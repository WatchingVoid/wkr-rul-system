using Dapper;
using Backend.Api.Data;
using Backend.Api.Models;

namespace Backend.Api.Services;

public sealed class TelemetryRepository
{
    private readonly DbConnectionFactory _db;

    public TelemetryRepository(DbConnectionFactory db) => _db = db;

    public async Task<long> InsertTelemetryAsync(TelemetryFrame dto, CuttingDerived derived, CancellationToken ct)
    {
        const string sql = """
        select wkr.insert_telemetry_spindle(
          @Ts, @MachineId, @ToolId,
          @SpindleRpm, @SpindleCurrentA, @SpindlePowerKw, @FeedMmMin, @Program, @CutFlag,
          @ToolDiameterMm, @CuttingSpeedMMin, @PowerFromTorqueKw, @TangentialForceN
        );
        """;

        using var conn = _db.Create();
        await conn.OpenAsync(ct);

        return await conn.ExecuteScalarAsync<long>(new CommandDefinition(sql, new
        {
            dto.Ts, dto.MachineId, dto.ToolId,
            dto.SpindleRpm, dto.SpindleCurrentA, dto.SpindlePowerKw, dto.FeedMmMin,
            Program = dto.Program, dto.CutFlag,
            ToolDiameterMm = dto.ToolDiameterMm,
            CuttingSpeedMMin = derived.CuttingSpeedMMin,
            PowerFromTorqueKw = derived.PowerFromTorqueKw,
            TangentialForceN = derived.TangentialForceN
        }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<MachineToolPair>> GetActivePairsAsync(CancellationToken ct)
    {
        const string sql = """
        select
          machine_id as MachineId,
          tool_id as ToolId,
          max(ts) as LastTs
        from wkr.telemetry_spindle
        where cut_flag = true
        group by machine_id, tool_id
        order by max(ts) desc;
        """;

        using var conn = _db.Create();
        await conn.OpenAsync(ct);

        var list = await conn.QueryAsync<MachineToolPair>(new CommandDefinition(sql, cancellationToken: ct));
        return list.ToList();
    }

    public async Task<IReadOnlyList<CutRow>> GetCutWindowAsync(string machineId, string toolId, int windowSize, CancellationToken ct)
    {
        // Берём из функции (она возвращает limit+order desc), переворачиваем в ASC
        const string sql = """
        select
          ts as Ts,
          spindle_rpm as SpindleRpm,
          spindle_current_a as SpindleCurrentA,
          spindle_power_kw as SpindlePowerKw
        from wkr.select_cut_window(@MachineId, @ToolId, @WindowSize);
        """;

        using var conn = _db.Create();
        await conn.OpenAsync(ct);

        var rows = (await conn.QueryAsync<CutRow>(new CommandDefinition(
            sql, new { MachineId = machineId, ToolId = toolId, WindowSize = windowSize }, cancellationToken: ct
        ))).ToList();

        rows.Reverse();
        return rows;
    }
}

public sealed class MachineToolPair
{
    public string MachineId { get; set; } = "";
    public string ToolId { get; set; } = "";
    public DateTime Ts { get; set; }
    public DateTime LastTs { get; set; }
}

public sealed class CutRow
{
    public DateTime Ts { get; set; }
    public int SpindleRpm { get; set; }
    public float SpindleCurrentA { get; set; }
    public float SpindlePowerKw { get; set; }
}