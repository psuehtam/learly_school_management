using Learly.API.Auth;
using Learly.API.Auth.Filters;
using Learly.API.Mapping;
using Learly.Application.Contracts.Calendario.Requests;
using Learly.Application.Contracts.Calendario.Responses;
using Learly.Application.Contracts.Common;
using Learly.Application.Services.Calendario;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Learly.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[SchoolUserOnly]
public sealed class CalendarioController : ControllerBase
{
    private readonly ICalendarioService _calendarioService;

    public CalendarioController(ICalendarioService calendarioService)
    {
        _calendarioService = calendarioService;
    }

    [HttpGet]
    [RequirePermission("VISUALIZAR_CALENDARIO")]
    public async Task<ActionResult<IReadOnlyList<EventoCalendarioResponse>>> ListarPorMes(
        [FromQuery] int mes,
        [FromQuery] int ano,
        CancellationToken cancellationToken)
    {
        var uc = AppUserContextMapper.From(HttpContext.GetUserContext());
        var eventos = await _calendarioService.ListarPorMesAsync(mes, ano, uc, cancellationToken);
        return Ok(eventos);
    }

    [HttpPost]
    [RequirePermission("GERENCIAR_CALENDARIO")]
    public async Task<IActionResult> Criar([FromBody] CriarEventoCalendarioRequest body, CancellationToken cancellationToken)
    {
        var uc = AppUserContextMapper.From(HttpContext.GetUserContext());
        var resultado = await _calendarioService.CriarAsync(body, uc, cancellationToken);
        if (!resultado.Success)
            return StatusCode(resultado.StatusCode, new MensagemResponse(resultado.Error ?? "Falha ao criar evento de calendario."));

        return StatusCode(201, resultado.Evento);
    }

    [HttpPut("{id:int}")]
    [RequirePermission("EDITAR_EVENTO_CALENDARIO")]
    public async Task<IActionResult> Editar(int id, [FromBody] EditarEventoCalendarioRequest body, CancellationToken cancellationToken)
    {
        var uc = AppUserContextMapper.From(HttpContext.GetUserContext());
        var resultado = await _calendarioService.EditarAsync(id, body, uc, cancellationToken);
        if (!resultado.Success)
            return StatusCode(resultado.StatusCode, new MensagemResponse(resultado.Error ?? "Falha ao editar evento de calendario."));

        return Ok(resultado.Evento);
    }

    [HttpDelete("{id:int}")]
    [RequirePermission("EXCLUIR_EVENTO_CALENDARIO")]
    public async Task<IActionResult> Excluir(int id, CancellationToken cancellationToken)
    {
        var uc = AppUserContextMapper.From(HttpContext.GetUserContext());
        var resultado = await _calendarioService.ExcluirAsync(id, uc, cancellationToken);
        if (!resultado.Success)
            return StatusCode(resultado.StatusCode, new MensagemResponse(resultado.Error ?? "Falha ao excluir evento de calendario."));

        return NoContent();
    }
}
