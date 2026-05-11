using Backend.Api.Models;
using Backend.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api.Controllers;

[ApiController]
[Route("api/telemetry")]
public sealed class TelemetryController : ControllerBase
{
    private readonly TelemetryRepository _repo;
    private readonly MachineStateResolver _stateResolver;

    public TelemetryController(
        TelemetryRepository repo,
        MachineStateResolver stateResolver)
    {
        _repo = repo;
        _stateResolver = stateResolver;
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] TelemetryFrame dto, CancellationToken ct)
    {
        var derived = CuttingMath.Compute(
            spindleRpm: dto.SpindleRpm,
            toolDiameterMm: dto.ToolDiameterMm,
            torqueNm: dto.SpindleTorqueNm,
            spindlePowerKw: dto.SpindlePowerKw
        );

        var machineState = _stateResolver.Resolve(dto);

        var id = await _repo.InsertTelemetryAsync(dto, derived, machineState, ct);

        return Ok(new
        {
            ok = true,
            id,
            machineState = machineState.MachineState,
            spindleState = machineState.SpindleState,
            stopRequired = machineState.StopRequired,
            stopReason = machineState.StopReason,
            controlAction = machineState.ControlAction
        });
    }
}