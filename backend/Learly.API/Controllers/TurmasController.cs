using Learly.API.Auth;
using Learly.API.Auth.Filters;
using Learly.API.Http;
using Learly.API.Mapping;
using Learly.Application.Contracts.Turmas.Requests;
using Learly.Application.Contracts.Turmas.Responses;
using Learly.Application.Services.Turmas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Learly.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[SchoolUserOnly]
public sealed class TurmasController : ControllerBase
{
    private readonly ITurmasService _turmas;

    public TurmasController(ITurmasService turmas)
    {
        _turmas = turmas;
    }

    [HttpGet]
    [RequirePermission("VISUALIZAR_TURMA")]
    public async Task<IActionResult> Listar([FromQuery] string? status, CancellationToken cancellationToken)
    {
        var resultado = await _turmas.ListarAsync(status, AppUserContextMapper.From(HttpContext.GetUserContext()), cancellationToken);
        return resultado.ToActionResult(this);
    }

    [HttpGet("{id:int}")]
    [RequirePermission("VISUALIZAR_TURMA")]
    public async Task<IActionResult> Obter(int id, CancellationToken cancellationToken)
    {
        var resultado = await _turmas.ObterPorIdAsync(id, AppUserContextMapper.From(HttpContext.GetUserContext()), cancellationToken);
        return resultado.ToActionResult(this);
    }

    [HttpPost]
    [RequirePermission("CRIAR_TURMA")]
    public async Task<IActionResult> Criar([FromBody] CriarTurmaRequest body, CancellationToken cancellationToken)
    {
        var resultado = await _turmas.CriarAsync(body, AppUserContextMapper.From(HttpContext.GetUserContext()), cancellationToken);
        return resultado.ToActionResult(this, created: true);
    }

    [HttpPut("{id:int}")]
    [RequirePermission("EDITAR_TURMA")]
    public async Task<IActionResult> Atualizar(int id, [FromBody] AtualizarTurmaRequest body, CancellationToken cancellationToken)
    {
        var resultado = await _turmas.AtualizarAsync(id, body, AppUserContextMapper.From(HttpContext.GetUserContext()), cancellationToken);
        return resultado.ToActionResult(this);
    }

    [HttpPost("{id:int}/ativar")]
    [RequirePermission("AGENDAR_TURMA")]
    public async Task<IActionResult> Ativar(int id, [FromBody] AtivarTurmaRequest body, CancellationToken cancellationToken)
    {
        var resultado = await _turmas.AtivarAsync(id, body, AppUserContextMapper.From(HttpContext.GetUserContext()), cancellationToken);
        return resultado.ToActionResult(this);
    }

    [HttpPost("{id:int}/concluir")]
    [RequirePermission("CONCLUIR_TURMA")]
    public async Task<IActionResult> Concluir(int id, CancellationToken cancellationToken)
    {
        var resultado = await _turmas.ConcluirAsync(id, AppUserContextMapper.From(HttpContext.GetUserContext()), cancellationToken);
        return resultado.ToActionResult(this);
    }

    [HttpPost("{id:int}/inativar")]
    [RequirePermission("INATIVAR_TURMA")]
    public async Task<IActionResult> Inativar(int id, CancellationToken cancellationToken)
    {
        var resultado = await _turmas.InativarAsync(id, AppUserContextMapper.From(HttpContext.GetUserContext()), cancellationToken);
        return resultado.ToActionResult(this);
    }

    [HttpGet("{id:int}/avaliacoes")]
    [RequirePermission("VISUALIZAR_TURMA")]
    public async Task<ActionResult<IReadOnlyList<AvaliacaoTurmaResponse>>> ListarAvaliacoes(
        int id,
        CancellationToken cancellationToken)
    {
        var uc = AppUserContextMapper.From(HttpContext.GetUserContext());
        var lista = await _turmas.ListarAvaliacoesAsync(id, uc, cancellationToken);
        return Ok(lista);
    }

    [HttpPost("{id:int}/avaliacoes")]
    [RequirePermission("VISUALIZAR_TURMA")]
    public async Task<IActionResult> SalvarAvaliacoes(
        int id,
        [FromBody] SalvarAvaliacoesTurmaRequest body,
        CancellationToken cancellationToken)
    {
        var resultado = await _turmas.SalvarAvaliacoesAsync(
            id,
            body,
            AppUserContextMapper.From(HttpContext.GetUserContext()),
            cancellationToken);
        if (!resultado.Ok)
        {
            return resultado.ToActionResult(this);
        }

        return NoContent();
    }
}
