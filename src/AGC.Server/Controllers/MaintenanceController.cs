using AGC.Server.Services;
using AGC.Shared.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AGC.Server.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/maintenance")]
public sealed class MaintenanceController(MaintenanceState maintenance) : ControllerBase
{
    [HttpGet("status")]
    public ActionResult<MaintenanceStatusDto> GetStatus()
        => Ok(new MaintenanceStatusDto(maintenance.IsActive, maintenance.Message, maintenance.ReopensAtUtc));
}
