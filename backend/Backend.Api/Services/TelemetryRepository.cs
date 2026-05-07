using Backend.Api.Data;
using Backend.Api.Models;
using Dapper;

namespace Backend.Api.Services;

public sealed class TelemetryRepository
{
    private readonly DbConnectionFactory _db;

    public TelemetryRepository(DbConnectionFactory db)
    {
        _db = db;
    }

    public async Task<long> InsertTelemetryAsync(
        TelemetryFrame dto,
        CuttingDerived derived,
        CancellationToken ct)
    {
        const string sql = """
            select wkr.insert_telemetry_spindle(
                @Ts,
                @MachineId,
                @ToolId,
                @SpindleRpm,
                @SpindleCurrentA,
                @SpindlePowerKw,
                @FeedMmMin,
                @Program,
                @CutFlag,
                @ToolDiameterMm,
                @CuttingSpeedMmin,
                @PowerFromTorqueKw,
                @TangentialForceN
            );
        """;

        using var conn = _db.Create();
        await conn.OpenAsync(ct);

        return await conn.ExecuteScalarAsync<long>(new CommandDefinition(
            sql,
            new
            {
                dto.Ts,
                dto.MachineId,
                dto.ToolId,
                dto.SpindleRpm,
                dto.SpindleCurrentA,
                dto.SpindlePowerKw,
                dto.FeedMmMin,
                dto.Program,
                dto.CutFlag,
                dto.ToolDiameterMm,
                derived.CuttingSpeedMmin,
                derived.PowerFromTorqueKw,
                derived.TangentialForceN
            },
            cancellationToken: ct
        ));
    }

    public async Task<IReadOnlyList<ActivePair>> GetActivePairsAsync(CancellationToken ct)
    {
        const string sql = """
            select
                machine_id as "MachineId",
                tool_id as "ToolId",
                max(ts) as "LastTs"
            from wkr.telemetry_spindle
            where cut_flag = true
            group by machine_id, tool_id
            order by max(ts) desc;
        """;

        using var conn = _db.Create();
        await conn.OpenAsync(ct);

        var rows = await conn.QueryAsync<ActivePair>(new CommandDefinition(
            sql,
            cancellationToken: ct
        ));

        return rows.ToList();
    }

    public async Task<IReadOnlyList<CutRow>> GetCutWindowAsync(
        string machineId,
        string toolId,
        int windowSize,
        CancellationToken ct)
    {
        const string sql = """
            select *
            from wkr.select_cut_window(@MachineId, @ToolId, @WindowSize);
        """;

        using var conn = _db.Create();
        await conn.OpenAsync(ct);

        var rows = await conn.QueryAsync<CutRow>(new CommandDefinition(
            sql,
            new
            {
                MachineId = machineId,
                ToolId = toolId,
                WindowSize = windowSize
            },
            cancellationToken: ct
        ));

        return rows.ToList();
    }
}

public sealed class ActivePair
{
    public string MachineId { get; set; } = "";
    public string ToolId { get; set; } = "";
    public DateTimeOffset LastTs { get; set; }
}