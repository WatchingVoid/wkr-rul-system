using Backend.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
public sealed class DashboardController : ControllerBase
{
    private readonly DashboardRepository _repo;

    public DashboardController(DashboardRepository repo)
    {
        _repo = repo;
    }

    [HttpGet("current")]
    public async Task<IActionResult> Current(
        [FromQuery] string machineId,
        [FromQuery] string toolId,
        [FromQuery] int eventLimit,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(machineId))
            return BadRequest(new { error = "machineId is required" });

        if (string.IsNullOrWhiteSpace(toolId))
            return BadRequest(new { error = "toolId is required" });

        var result = await _repo.GetCurrentAsync(
            machineId: machineId,
            toolId: toolId,
            eventLimit: eventLimit <= 0 ? 20 : eventLimit,
            ct: ct);

        return Ok(result);
    }

    [HttpGet("last-telemetry")]
    public async Task<IActionResult> LastTelemetry(
        [FromQuery] string machineId,
        [FromQuery] string toolId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(machineId))
            return BadRequest(new { error = "machineId is required" });

        if (string.IsNullOrWhiteSpace(toolId))
            return BadRequest(new { error = "toolId is required" });

        var result = await _repo.GetLastTelemetryAsync(machineId, toolId, ct);
        return Ok(result);
    }

    [HttpGet("telemetry-history")]
    public async Task<IActionResult> TelemetryHistory(
        [FromQuery] string machineId,
        [FromQuery] string toolId,
        [FromQuery] int limit,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(machineId))
            return BadRequest(new { error = "machineId is required" });

        if (string.IsNullOrWhiteSpace(toolId))
            return BadRequest(new { error = "toolId is required" });

        var result = await _repo.GetTelemetryHistoryAsync(
            machineId: machineId,
            toolId: toolId,
            limit: limit <= 0 ? 100 : limit,
            ct: ct);

        return Ok(result);
    }

    [HttpGet("last-rul")]
    public async Task<IActionResult> LastRul(
        [FromQuery] string machineId,
        [FromQuery] string toolId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(machineId))
            return BadRequest(new { error = "machineId is required" });

        if (string.IsNullOrWhiteSpace(toolId))
            return BadRequest(new { error = "toolId is required" });

        var result = await _repo.GetLastRulAsync(machineId, toolId, ct);
        return Ok(result);
    }

    [HttpGet("rul-history")]
    public async Task<IActionResult> RulHistory(
        [FromQuery] string machineId,
        [FromQuery] string toolId,
        [FromQuery] int limit,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(machineId))
            return BadRequest(new { error = "machineId is required" });

        if (string.IsNullOrWhiteSpace(toolId))
            return BadRequest(new { error = "toolId is required" });

        var result = await _repo.GetRulHistoryAsync(
            machineId: machineId,
            toolId: toolId,
            limit: limit <= 0 ? 100 : limit,
            ct: ct);

        return Ok(result);
    }

    [HttpGet("last-alarm")]
    public async Task<IActionResult> LastAlarm(
        [FromQuery] string machineId,
        [FromQuery] string toolId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(machineId))
            return BadRequest(new { error = "machineId is required" });

        if (string.IsNullOrWhiteSpace(toolId))
            return BadRequest(new { error = "toolId is required" });

        var result = await _repo.GetLastAlarmAsync(machineId, toolId, ct);
        return Ok(result);
    }

    [HttpGet("machine-events")]
    public async Task<IActionResult> MachineEvents(
        [FromQuery] string machineId,
        [FromQuery] int limit,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(machineId))
            return BadRequest(new { error = "machineId is required" });

        var result = await _repo.GetMachineEventsAsync(
            machineId: machineId,
            limit: limit <= 0 ? 50 : limit,
            ct: ct);

        return Ok(result);
    }
}