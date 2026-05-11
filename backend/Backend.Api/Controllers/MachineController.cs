using Backend.Api.Data;
using Dapper;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api.Controllers;

[ApiController]
[Route("api/machine")]
public sealed class MachineController : ControllerBase
{
    private readonly DbConnectionFactory _factory;

    public MachineController(DbConnectionFactory factory)
    {
        _factory = factory;
    }

    [HttpGet("last")]
    public async Task<IActionResult> Last([FromQuery] string machineId, CancellationToken ct)
    {
        await using var conn = _factory.Create();

        var sql = """
            select *
            from wkr.get_last_machine_state(@MachineId);
            """;

        var row = await conn.QueryFirstOrDefaultAsync(
            new CommandDefinition(
                sql,
                new { MachineId = machineId },
                cancellationToken: ct));

        return Ok(row);
    }

    [HttpGet("events")]
    public async Task<IActionResult> Events(
        [FromQuery] string machineId,
        [FromQuery] int limit,
        CancellationToken ct)
    {
        await using var conn = _factory.Create();

        var sql = """
            select *
            from wkr.get_machine_events(@MachineId, @Limit);
            """;

        var rows = await conn.QueryAsync(
            new CommandDefinition(
                sql,
                new
                {
                    MachineId = machineId,
                    Limit = limit <= 0 ? 20 : limit
                },
                cancellationToken: ct));

        return Ok(rows);
    }
}