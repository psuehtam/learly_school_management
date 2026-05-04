using Learly.API.Auth;
using Learly.API.Auth.Filters;
using Learly.API.Mapping;
using Learly.Application.Contracts.Common;
using Learly.Application.Contracts.Compromissos.Requests;
using Learly.Application.Contracts.Compromissos.Responses;
using Learly.Application.Services.Compromissos;
using Learly.Application.Services.Usuarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Learly.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[SchoolUserOnly]
public sealed class CompromissosController : ControllerBase
{
    private readonly ICompromissosService _compromissos;
    private readonly IUsuariosService _usuarios;

    public CompromissosController(ICompromissosService compromissos, IUsuariosService usuarios)
    {
        _compromissos = compromissos;
        _usuarios = usuarios;
    }

    [HttpGet]
    [RequirePermission("VISUALIZAR_COMPROMISSOS")]
    public async Task<ActionResult<IReadOnlyList<CompromissoResponse>>> Listar(CancellationToken cancellationToken)
    {
        var uc = AppUserContextMapper.From(HttpContext.GetUserContext());
        var result = await _compromissos.ListarMeusAsync(uc, cancellationToken);
        return Ok(result);
    }

    [HttpGet("agenda-global")]
    [RequirePermission("VISUALIZAR_AGENDA_GLOBAL")]
    public async Task<ActionResult<IReadOnlyList<CompromissoResponse>>> ListarAgendaGlobal(
        [FromQuery] DateOnly data,
        [FromQuery] int? usuarioId,
        CancellationToken cancellationToken)
    {
        var uc = AppUserContextMapper.From(HttpContext.GetUserContext());
        var result = await _compromissos.ListarAgendaGlobalAsync(data, usuarioId, uc, cancellationToken);
        return Ok(result);
    }

    [HttpGet("participantes")]
    [RequirePermission("CRIAR_COMPROMISSO", "EDITAR_COMPROMISSO", "VISUALIZAR_COMPROMISSOS")]
    public async Task<IActionResult> ListarParticipantes(CancellationToken cancellationToken)
    {
        var uc = AppUserContextMapper.From(HttpContext.GetUserContext());
        var usuarios = await _usuarios.ListarMinhaEscolaAsync(uc, cancellationToken);
        var permitidos = usuarios
            .Where(u => string.Equals(u.Status, "Ativo", StringComparison.OrdinalIgnoreCase))
            .Where(u =>
                string.Equals(u.PerfilNome, "Secretaria", StringComparison.OrdinalIgnoreCase)
                || string.Equals(u.PerfilNome, "Coordenador", StringComparison.OrdinalIgnoreCase)
                || string.Equals(u.PerfilNome, "Professor", StringComparison.OrdinalIgnoreCase)
                || string.Equals(u.PerfilNome, "Administrador", StringComparison.OrdinalIgnoreCase))
            .Select(u => new { u.Id, u.NomeCompleto, u.PerfilNome })
            .ToList();

        return Ok(permitidos);
    }

    [HttpPost]
    [RequirePermission("CRIAR_COMPROMISSO")]
    public async Task<IActionResult> Criar([FromBody] CriarCompromissoRequest request, CancellationToken cancellationToken)
    {
        var uc = AppUserContextMapper.From(HttpContext.GetUserContext());
        var result = await _compromissos.CriarAsync(request, uc, cancellationToken);
        if (!result.Success)
            return StatusCode(result.StatusCode, new MensagemResponse(result.Error ?? "Falha ao criar compromisso."));

        return StatusCode(201, result.Item);
    }

    [HttpPut("{id:int}")]
    [RequirePermission("EDITAR_COMPROMISSO")]
    public async Task<IActionResult> Editar(int id, [FromBody] EditarCompromissoRequest request, CancellationToken cancellationToken)
    {
        var uc = AppUserContextMapper.From(HttpContext.GetUserContext());
        var result = await _compromissos.EditarAsync(id, request, uc, cancellationToken);
        if (!result.Success)
            return StatusCode(result.StatusCode, new MensagemResponse(result.Error ?? "Falha ao editar compromisso."));

        return Ok(result.Item);
    }

    [HttpPost("{id:int}/cancelar")]
    [RequirePermission("EXCLUIR_COMPROMISSO")]
    public async Task<IActionResult> Cancelar(
        int id,
        [FromBody] CancelarCompromissoRequest request,
        CancellationToken cancellationToken)
    {
        var uc = AppUserContextMapper.From(HttpContext.GetUserContext());
        var result = await _compromissos.CancelarAsync(id, request.Motivo, uc, cancellationToken);
        if (!result.Success)
            return StatusCode(result.StatusCode, new MensagemResponse(result.Error ?? "Falha ao cancelar compromisso."));

        return NoContent();
    }
}
