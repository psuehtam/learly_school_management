using Learly.API.Auth;
using Learly.API.Auth.Filters;
using Learly.API.Mapping;
using Learly.Application.Contracts.Common;
using Learly.Application.Contracts.Dashboard;
using Learly.Application.Services.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Learly.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[SchoolUserOnly]
public sealed class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboard;

    public DashboardController(IDashboardService dashboard)
    {
        _dashboard = dashboard;
    }

    [HttpGet]
    [RequirePermission("VISUALIZAR_DASHBOARD_GERAL")]
    public async Task<ActionResult<DashboardGeralResponse>> ObterGeral(
        [FromQuery] string? data,
        CancellationToken cancellationToken)
    {
        DateOnly? dataRef = null;
        if (!string.IsNullOrWhiteSpace(data))
        {
            if (!DateOnly.TryParse(data, out var parsed))
            {
                return BadRequest(new MensagemResponse("Data invalida. Use formato yyyy-MM-dd."));
            }

            dataRef = parsed;
        }

        var uc = AppUserContextMapper.From(HttpContext.GetUserContext());
        var resultado = await _dashboard.ObterGeralAsync(uc, dataRef, cancellationToken);
        if (resultado is null)
        {
            return Forbid();
        }

        return Ok(resultado);
    }
}
