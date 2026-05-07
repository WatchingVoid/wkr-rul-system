using Backend.Api.Models;
using Backend.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api.Controllers;

[ApiController]
[Route("api/telemetry")]
public sealed class TelemetryController : ControllerBase
{
    private readonly TelemetryRepository _repo;

    public TelemetryController(TelemetryRepository repo)
    {
        _repo = repo;
    }

    [HttpPost]
    public async Task<IActionResult> Post(
        [FromBody] TelemetryFrame dto,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.MachineId))
            return BadRequest("machineId is required");

        if (string.IsNullOrWhiteSpace(dto.ToolId))
            return BadRequest("toolId is required");

        if (dto.SpindleRpm < 0)
            return BadRequest("spindleRpm must be >= 0");

        if (dto.SpindlePowerKw < 0)
            return BadRequest("spindlePowerKw must be >= 0");

        var derived = CuttingMath.Compute(
            spindleRpm: dto.SpindleRpm,
            toolDiameterMm: dto.ToolDiameterMm,
            torqueNm: dto.SpindleTorqueNm,
            spindlePowerKw: dto.SpindlePowerKw
        );

        var id = await _repo.InsertTelemetryAsync(dto, derived, ct);

        return Ok(new
        {
            ok = true,
            id,
            derived = new
            {
                derived.CuttingSpeedMmin,
                derived.PowerFromTorqueKw,
                derived.TangentialForceN
            }
        });
    }
}