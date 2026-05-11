using Backend.Api.Data;
using Backend.Api.Models;
using Dapper;

namespace Backend.Api.Services;

public sealed class TelemetryRepository
{
    private readonly DbConnectionFactory _factory;

    public TelemetryRepository(DbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<long> InsertTelemetryAsync(
        TelemetryFrame frame,
        CuttingDerived derived,
        MachineStateInfo state,
        CancellationToken ct)
    {
        await using var conn = _factory.Create();

        var sql = """
            select wkr.insert_telemetry_spindle_v2(
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
                @TangentialForceN,
                @MachineState,
                @SpindleState,
                @StopRequired,
                @StopReason,
                @ControlAction
            );
            """;

        var id = await conn.QuerySingleAsync<long>(
            new CommandDefinition(
                sql,
                new
                {
                    frame.Ts,
                    frame.MachineId,
                    frame.ToolId,
                    frame.SpindleRpm,
                    frame.SpindleCurrentA,
                    frame.SpindlePowerKw,
                    frame.FeedMmMin,
                    frame.Program,
                    frame.CutFlag,
                    frame.ToolDiameterMm,

                    derived.CuttingSpeedMmin,
                    derived.PowerFromTorqueKw,
                    derived.TangentialForceN,

                    state.MachineState,
                    state.SpindleState,
                    state.StopRequired,
                    state.StopReason,
                    state.ControlAction
                },
                cancellationToken: ct));

        if (ShouldWriteMachineEvent(state))
        {
            await InsertMachineEventAsync(frame, state, ct);
        }

        return id;
    }

    private async Task<long> InsertMachineEventAsync(
        TelemetryFrame frame,
        MachineStateInfo state,
        CancellationToken ct)
    {
        await using var conn = _factory.Create();

        var sql = """
            select wkr.insert_machine_event(
                @Ts,
                @MachineId,
                @ToolId,
                @EventCode,
                @EventLevel,
                @EventMessage,
                @MachineState,
                @SpindleState,
                @StopReason,
                @ControlAction
            );
            """;

        return await conn.QuerySingleAsync<long>(
            new CommandDefinition(
                sql,
                new
                {
                    frame.Ts,
                    frame.MachineId,
                    frame.ToolId,
                    state.EventCode,
                    state.EventLevel,
                    state.EventMessage,
                    state.MachineState,
                    state.SpindleState,
                    state.StopReason,
                    state.ControlAction
                },
                cancellationToken: ct));
    }

    private static bool ShouldWriteMachineEvent(MachineStateInfo state)
    {
        // Пока пишем только важные события.
        // Обычные MACHINE_CUTTING / MACHINE_IDLE не пишем, чтобы не засорять журнал.
        return state.EventLevel > 0
               || state.StopRequired
               || string.Equals(state.EventCode, "MACHINE_STOP_RUL_CRITICAL", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<IReadOnlyList<ActiveMachineToolPair>> GetActivePairsAsync(CancellationToken ct)
    {
        await using var conn = _factory.Create();

        var sql = """
            select
                t.machine_id as MachineId,
                t.tool_id as ToolId,
                max(t.ts) as LastTs
            from wkr.telemetry_spindle t
            where t.cut_flag = true
            group by t.machine_id, t.tool_id
            order by max(t.ts) desc;
            """;

        var rows = await conn.QueryAsync<ActiveMachineToolPair>(
            new CommandDefinition(sql, cancellationToken: ct));

        return rows.ToList();
    }

    public async Task<IReadOnlyList<CutWindowRow>> GetCutWindowAsync(
        string machineId,
        string toolId,
        int windowSize,
        CancellationToken ct)
    {
        await using var conn = _factory.Create();

        var sql = """
            select
                t.ts as Ts,
                t.spindle_rpm as SpindleRpm,
                t.spindle_current_a as SpindleCurrentA,
                t.spindle_power_kw as SpindlePowerKw,
                t.cutting_speed_mmin as CuttingSpeedMmin,
                t.tangential_force_n as TangentialForceN
            from (
                select *
                from wkr.telemetry_spindle
                where machine_id = @MachineId
                  and tool_id = @ToolId
                  and cut_flag = true
                order by ts desc
                limit @WindowSize
            ) t
            order by t.ts asc;
            """;

        var rows = await conn.QueryAsync<CutWindowRow>(
            new CommandDefinition(
                sql,
                new
                {
                    MachineId = machineId,
                    ToolId = toolId,
                    WindowSize = windowSize
                },
                cancellationToken: ct));

        return rows.ToList();
    }
}

public sealed class ActiveMachineToolPair
{
    public string MachineId { get; init; } = "";
    public string ToolId { get; init; } = "";
    public DateTimeOffset LastTs { get; init; }
}

public sealed class CutWindowRow
{
    public DateTimeOffset Ts { get; init; }

    public int SpindleRpm { get; init; }
    public float SpindleCurrentA { get; init; }
    public float SpindlePowerKw { get; init; }

    public float? CuttingSpeedMmin { get; init; }
    public float? TangentialForceN { get; init; }
}