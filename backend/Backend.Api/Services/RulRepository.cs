using Dapper;
using Backend.Api.Data;

namespace Backend.Api.Services;

public sealed class RulRepository
{
    private readonly DbConnectionFactory _db;
    public RulRepository(DbConnectionFactory db) => _db = db;

    public async Task<long> InsertPredictionAsync(DateTimeOffset ts, string machineId, string toolId,
        float rulMinutes, int alarmLevel, string modelVersion, CancellationToken ct)
    {
        const string sql = """
            select wkr.insert_rul_prediction(@Ts, @MachineId, @ToolId, @RulMinutes, @AlarmLevel, @ModelVersion);
        """;

        using var conn = _db.Create();
        await conn.OpenAsync(ct);

        return await conn.ExecuteScalarAsync<long>(new CommandDefinition(
            sql,
            new { Ts = ts, MachineId = machineId, ToolId = toolId, RulMinutes = rulMinutes, AlarmLevel = alarmLevel, ModelVersion = modelVersion },
            cancellationToken: ct
        ));
    }

    public async Task<LastRulDto?> GetLastAsync(string machineId, string toolId, CancellationToken ct)
    {
        const string sql = """
            select * from wkr.get_last_rul(@MachineId, @ToolId);
        """;

        using var conn = _db.Create();
        await conn.OpenAsync(ct);

        return await conn.QueryFirstOrDefaultAsync<LastRulDto>(new CommandDefinition(
            sql, new { MachineId = machineId, ToolId = toolId }, cancellationToken: ct
        ));
    }
}

public sealed class LastRulDto
{
    public DateTime Ts { get; set; }
    public float Rul_Minutes { get; set; }
    public int Alarm_Level { get; set; }
    public string Model_Version { get; set; } = "";
}