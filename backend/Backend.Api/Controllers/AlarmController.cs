using Backend.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api.Controllers;

[ApiController]
[Route("api/alarms")]
public sealed class AlarmController : ControllerBase
{
    private readonly AlarmRepository _repo;

    public AlarmController(AlarmRepository repo)
    {
        _repo = repo;
    }

    [HttpGet("last")]
    public async Task<IActionResult> Last(
        [FromQuery] string machineId,
        [FromQuery] string toolId,
        CancellationToken ct)
    {
        var result = await _repo.GetLastAlarmAsync(machineId, toolId, ct);
        return Ok(result);
    }
}