using Microsoft.AspNetCore.Mvc;
using Backend.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Backend.Api.Controllers;

[ApiController]
[Route("api/rul")]
public sealed class RulController : ControllerBase
{
    private readonly AppDbContext _db;

    public RulController(AppDbContext db) => _db = db;

    [HttpGet("last")]
    public async Task<IActionResult> Last(CancellationToken ct)
    {
        var last = await _db.RulPredictions.OrderByDescending(x => x.Ts).FirstOrDefaultAsync(ct);
        return Ok(last);
    }
}