using Backend.Api.Data;
using Dapper;

namespace Backend.Api.Services;

public sealed class AlarmRepository
{
    private readonly DbConnectionFactory _db;

    public AlarmRepository(DbConnectionFactory db)
    {
        _db = db;
    }

    public async Task<long> InsertAlarmAsync(
        DateTimeOffset ts,
        string machineId,
        string toolId,
        float rulMinutes,
        int alarmLevel,
        string alarmCode,
        string alarmMessage,
        string requiredAction,
        string modelVersion,
        CancellationToken ct)
    {
        const string sql = """
            select wkr.insert_alarm_event(
                @Ts,
                @MachineId,
                @ToolId,
                @RulMinutes,
                @AlarmLevel,
                @AlarmCode,
                @AlarmMessage,
                @RequiredAction,
                @ModelVersion
            );
        """;

        using var conn = _db.Create();
        await conn.OpenAsync(ct);

        return await conn.ExecuteScalarAsync<long>(new CommandDefinition(
            sql,
            new
            {
                Ts = ts,
                MachineId = machineId,
                ToolId = toolId,
                RulMinutes = rulMinutes,
                AlarmLevel = alarmLevel,
                AlarmCode = alarmCode,
                AlarmMessage = alarmMessage,
                RequiredAction = requiredAction,
                ModelVersion = modelVersion
            },
            cancellationToken: ct
        ));
    }

    public async Task<LastAlarmDto?> GetLastAlarmAsync(
        string machineId,
        string toolId,
        CancellationToken ct)
    {
        const string sql = """
            select *
            from wkr.get_last_alarm(@MachineId, @ToolId);
        """;

        using var conn = _db.Create();
        await conn.OpenAsync(ct);

        return await conn.QueryFirstOrDefaultAsync<LastAlarmDto>(new CommandDefinition(
            sql,
            new
            {
                MachineId = machineId,
                ToolId = toolId
            },
            cancellationToken: ct
        ));
    }
}

public sealed class LastAlarmDto
{
    public DateTimeOffset Ts { get; set; }
    public string MachineId { get; set; } = "";
    public string ToolId { get; set; } = "";
    public float RulMinutes { get; set; }
    public int AlarmLevel { get; set; }
    public string AlarmCode { get; set; } = "";
    public string AlarmMessage { get; set; } = "";
    public string RequiredAction { get; set; } = "";
    public bool IsActive { get; set; }
    public string ModelVersion { get; set; } = "";
}