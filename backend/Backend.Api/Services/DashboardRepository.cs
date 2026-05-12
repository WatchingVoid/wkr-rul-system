using Backend.Api.Data;
using Backend.Api.Models.Dashboard;
using Dapper;

namespace Backend.Api.Services;

public sealed class DashboardRepository
{
    private readonly DbConnectionFactory _factory;

    public DashboardRepository(DbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<DashboardCurrentDto> GetCurrentAsync(
        string machineId,
        string toolId,
        int eventLimit,
        CancellationToken ct)
    {
        var lastTelemetry = await GetLastTelemetryAsync(machineId, toolId, ct);
        var lastPrediction = await GetLastRulAsync(machineId, toolId, ct);
        var lastAlarm = await GetLastAlarmAsync(machineId, toolId, ct);
        var events = await GetMachineEventsAsync(machineId, eventLimit, ct);

        return new DashboardCurrentDto
        {
            MachineId = machineId,
            ToolId = toolId,
            LastTelemetry = lastTelemetry,
            LastPrediction = lastPrediction,
            LastAlarm = lastAlarm,
            MachineEvents = events
        };
    }

    public async Task<DashboardTelemetryPointDto?> GetLastTelemetryAsync(
        string machineId,
        string toolId,
        CancellationToken ct)
    {
        await using var conn = _factory.Create();

        var sql = """
            select
                t.id as "Id",
                t.ts as "Ts",
                t.machine_id as "MachineId",
                t.tool_id as "ToolId",

                t.spindle_rpm as "SpindleRpm",
                t.spindle_current_a as "SpindleCurrentA",
                t.spindle_power_kw as "SpindlePowerKw",
                t.feed_mm_min as "FeedMmMin",

                t.program as "Program",
                t.cut_flag as "CutFlag",

                t.tool_diameter_mm as "ToolDiameterMm",
                t.cutting_speed_mmin as "CuttingSpeedMmin",
                t.power_from_torque_kw as "PowerFromTorqueKw",
                t.tangential_force_n as "TangentialForceN",

                t.machine_state as "MachineState",
                t.spindle_state as "SpindleState",
                t.stop_required as "StopRequired",
                t.stop_reason as "StopReason",
                t.control_action as "ControlAction",

                case
                    when t.cut_flag = true and t.spindle_rpm > 0 then true
                    else false
                end as "IsCutting",

                case
                    when t.spindle_rpm <= 0 and t.cut_flag = false then true
                    else false
                end as "IsStopped"

            from wkr.telemetry_spindle t
            where t.machine_id = @MachineId
              and t.tool_id = @ToolId
            order by t.ts desc
            limit 1;
            """;

        return await conn.QueryFirstOrDefaultAsync<DashboardTelemetryPointDto>(
            new CommandDefinition(
                sql,
                new
                {
                    MachineId = machineId,
                    ToolId = toolId
                },
                cancellationToken: ct));
    }

    public async Task<IReadOnlyList<DashboardTelemetryPointDto>> GetTelemetryHistoryAsync(
        string machineId,
        string toolId,
        int limit,
        CancellationToken ct)
    {
        await using var conn = _factory.Create();

        var safeLimit = NormalizeLimit(limit, 20, 500);

        var sql = """
            select *
            from (
                select
                    t.id as "Id",
                    t.ts as "Ts",
                    t.machine_id as "MachineId",
                    t.tool_id as "ToolId",

                    t.spindle_rpm as "SpindleRpm",
                    t.spindle_current_a as "SpindleCurrentA",
                    t.spindle_power_kw as "SpindlePowerKw",
                    t.feed_mm_min as "FeedMmMin",

                    t.program as "Program",
                    t.cut_flag as "CutFlag",

                    t.tool_diameter_mm as "ToolDiameterMm",
                    t.cutting_speed_mmin as "CuttingSpeedMmin",
                    t.power_from_torque_kw as "PowerFromTorqueKw",
                    t.tangential_force_n as "TangentialForceN",

                    t.machine_state as "MachineState",
                    t.spindle_state as "SpindleState",
                    t.stop_required as "StopRequired",
                    t.stop_reason as "StopReason",
                    t.control_action as "ControlAction",

                    case
                        when t.cut_flag = true and t.spindle_rpm > 0 then true
                        else false
                    end as "IsCutting",

                    case
                        when t.spindle_rpm <= 0 and t.cut_flag = false then true
                        else false
                    end as "IsStopped"

                from wkr.telemetry_spindle t
                where t.machine_id = @MachineId
                  and t.tool_id = @ToolId
                order by t.ts desc
                limit @Limit
            ) q
            order by q."Ts" asc;
            """;

        var rows = await conn.QueryAsync<DashboardTelemetryPointDto>(
            new CommandDefinition(
                sql,
                new
                {
                    MachineId = machineId,
                    ToolId = toolId,
                    Limit = safeLimit
                },
                cancellationToken: ct));

        return rows.ToList();
    }

    public async Task<DashboardRulPointDto?> GetLastRulAsync(
        string machineId,
        string toolId,
        CancellationToken ct)
    {
        await using var conn = _factory.Create();

        var sql = """
            select
                r.id as "Id",
                r.ts as "Ts",
                r.machine_id as "MachineId",
                r.tool_id as "ToolId",
                r.rul_minutes as "RulMinutes",
                r.alarm_level as "AlarmLevel",
                r.alarm_code as "AlarmCode",
                r.state as "State",
                r.message as "Message",
                r.required_action as "RequiredAction",
                r.model_version as "ModelVersion"
            from wkr.rul_predictions r
            where r.machine_id = @MachineId
              and r.tool_id = @ToolId
            order by r.ts desc
            limit 1;
            """;

        return await conn.QueryFirstOrDefaultAsync<DashboardRulPointDto>(
            new CommandDefinition(
                sql,
                new
                {
                    MachineId = machineId,
                    ToolId = toolId
                },
                cancellationToken: ct));
    }

    public async Task<IReadOnlyList<DashboardRulPointDto>> GetRulHistoryAsync(
        string machineId,
        string toolId,
        int limit,
        CancellationToken ct)
    {
        await using var conn = _factory.Create();

        var safeLimit = NormalizeLimit(limit, 20, 500);

        var sql = """
            select *
            from (
                select
                    r.id as "Id",
                    r.ts as "Ts",
                    r.machine_id as "MachineId",
                    r.tool_id as "ToolId",
                    r.rul_minutes as "RulMinutes",
                    r.alarm_level as "AlarmLevel",
                    r.alarm_code as "AlarmCode",
                    r.state as "State",
                    r.message as "Message",
                    r.required_action as "RequiredAction",
                    r.model_version as "ModelVersion"
                from wkr.rul_predictions r
                where r.machine_id = @MachineId
                  and r.tool_id = @ToolId
                order by r.ts desc
                limit @Limit
            ) q
            order by q."Ts" asc;
            """;

        var rows = await conn.QueryAsync<DashboardRulPointDto>(
            new CommandDefinition(
                sql,
                new
                {
                    MachineId = machineId,
                    ToolId = toolId,
                    Limit = safeLimit
                },
                cancellationToken: ct));

        return rows.ToList();
    }

    public async Task<DashboardAlarmDto?> GetLastAlarmAsync(
        string machineId,
        string toolId,
        CancellationToken ct)
    {
        await using var conn = _factory.Create();

        var sql = """
            select
                a.id as "Id",
                a.ts as "Ts",
                a.machine_id as "MachineId",
                a.tool_id as "ToolId",
                a.rul_minutes as "RulMinutes",
                a.alarm_level as "AlarmLevel",
                a.alarm_code as "AlarmCode",
                a.alarm_message as "AlarmMessage"
            from wkr.alarm_events a
            where a.machine_id = @MachineId
              and a.tool_id = @ToolId
            order by a.ts desc
            limit 1;
            """;

        return await conn.QueryFirstOrDefaultAsync<DashboardAlarmDto>(
            new CommandDefinition(
                sql,
                new
                {
                    MachineId = machineId,
                    ToolId = toolId
                },
                cancellationToken: ct));
    }

    public async Task<IReadOnlyList<DashboardMachineEventDto>> GetMachineEventsAsync(
        string machineId,
        int limit,
        CancellationToken ct)
    {
        await using var conn = _factory.Create();

        var safeLimit = NormalizeLimit(limit, 20, 200);

        var sql = """
            select
                e.id as "Id",
                e.ts as "Ts",
                e.machine_id as "MachineId",
                e.tool_id as "ToolId",
                e.event_code as "EventCode",
                e.event_level as "EventLevel",
                e.event_message as "EventMessage",
                e.machine_state as "MachineState",
                e.spindle_state as "SpindleState",
                e.stop_reason as "StopReason",
                e.control_action as "ControlAction"
            from wkr.machine_events e
            where e.machine_id = @MachineId
            order by e.ts desc
            limit @Limit;
            """;

        var rows = await conn.QueryAsync<DashboardMachineEventDto>(
            new CommandDefinition(
                sql,
                new
                {
                    MachineId = machineId,
                    Limit = safeLimit
                },
                cancellationToken: ct));

        return rows.ToList();
    }

    private static int NormalizeLimit(int limit, int defaultValue, int maxValue)
    {
        if (limit <= 0)
            return defaultValue;

        return Math.Min(limit, maxValue);
    }
}