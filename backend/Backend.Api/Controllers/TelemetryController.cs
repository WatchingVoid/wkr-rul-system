using Microsoft.AspNetCore.Mvc;
using Backend.Api.Data;
using Backend.Api.Entities;
using Backend.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Api.Controllers;

[ApiController]
[Route("api/telemetry")]
public sealed class TelemetryController : ControllerBase
{
    private readonly AppDbContext _db;

    public TelemetryController(AppDbContext db) => _db = db;

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] TelemetryFrame dto, CancellationToken ct)
    {
        _db.TelemetrySpindle.Add(new TelemetrySpindleEntity
        {
            Ts = dto.Ts,
            MachineId = dto.MachineId,
            ToolId = dto.ToolId,
            SpindleRpm = dto.SpindleRpm,
            SpindleCurrentA = dto.SpindleCurrentA,
            SpindlePowerKw = dto.SpindlePowerKw,
            FeedMmMin = dto.FeedMmMin,
            Program = dto.Program,
            CutFlag = dto.CutFlag
        });

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    [HttpGet("last")]
    public async Task<IActionResult> Last(CancellationToken ct)
    {
        var last = await _db.TelemetrySpindle.OrderByDescending(x => x.Ts).FirstOrDefaultAsync(ct);
        return Ok(last);
    }
}