using Backend.Api.Models;
using Backend.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api.Controllers;

[ApiController]
[Route("api/telemetry")]
public sealed class TelemetryController : ControllerBase
{
    private readonly TelemetryRepository _repo;

    public TelemetryController(TelemetryRepository repo) => _repo = repo;

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] TelemetryFrame dto, CancellationToken ct)
    {
        var derived = CuttingMath.Compute(
            spindleRpm: dto.SpindleRpm,
            toolDiameterMm: dto.ToolDiameterMm,
            torqueNm: dto.SpindleTorqueNm,
            spindlePowerKw: dto.SpindlePowerKw
        );

        var id = await _repo.InsertTelemetryAsync(dto, derived, ct);
        return Ok(new { ok = true, id });
    }
}