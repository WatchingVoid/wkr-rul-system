using Backend.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api.Controllers;

[ApiController]
[Route("api/rul")]
public sealed class RulController : ControllerBase
{
    private readonly RulRepository _repo;

    public RulController(RulRepository repo) => _repo = repo;

    [HttpGet("last")]
    public async Task<IActionResult> Last([FromQuery] string machineId, [FromQuery] string toolId, CancellationToken ct)
        => Ok(await _repo.GetLastAsync(machineId, toolId, ct));
}