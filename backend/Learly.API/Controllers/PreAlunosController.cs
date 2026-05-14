using Learly.API.Auth;
using Learly.API.Auth.Filters;
using Learly.API.Http;
using Learly.API.Mapping;
using Learly.Application.Contracts.PreAlunos.Requests;
using Learly.Application.Services.PreAlunos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Learly.API.Controllers;

[ApiController]
[Route("api/pre-alunos")]
[Authorize]
[SchoolUserOnly]
public sealed class PreAlunosController : ControllerBase
{
    private readonly IPreAlunosService _preAlunosService;

    public PreAlunosController(IPreAlunosService preAlunosService)
    {
        _preAlunosService = preAlunosService;
    }

    [HttpGet("livros-interesse")]
    [RequirePermission("VISUALIZAR_PRE_ALUNO")]
    public async Task<IActionResult> ListarLivrosInteresse(CancellationToken cancellationToken)
    {
        var uc = AppUserContextMapper.From(HttpContext.GetUserContext());
        var r = await _preAlunosService.ListarCatalogoLivrosInteresseAsync(uc, cancellationToken);
        return r.ToActionResult(this);
    }

    [HttpGet]
    [RequirePermission("VISUALIZAR_PRE_ALUNO")]
    public async Task<IActionResult> Listar([FromQuery] ListarPreAlunosQuery? filtro, CancellationToken cancellationToken)
    {
        var uc = AppUserContextMapper.From(HttpContext.GetUserContext());
        var r = await _preAlunosService.ListarAsync(filtro ?? new ListarPreAlunosQuery(), uc, cancellationToken);
        return r.ToActionResult(this);
    }

    [HttpGet("{id:int}")]
    [RequirePermission("VISUALIZAR_PRE_ALUNO")]
    public async Task<IActionResult> Obter(int id, CancellationToken cancellationToken)
    {
        var uc = AppUserContextMapper.From(HttpContext.GetUserContext());
        var r = await _preAlunosService.ObterPorIdAsync(id, uc, cancellationToken);
        return r.ToActionResult(this);
    }

    [HttpPost]
    [RequirePermission("CRIAR_PRE_ALUNO")]
    public async Task<IActionResult> Criar([FromBody] CriarPreAlunoRequest body, CancellationToken cancellationToken)
    {
        var uc = AppUserContextMapper.From(HttpContext.GetUserContext());
        var r = await _preAlunosService.CriarAsync(body, uc, cancellationToken);
        return r.ToActionResult(this);
    }

    [HttpPatch("{id:int}/submeter-aprovacao")]
    [RequirePermission("EDITAR_PRE_ALUNO", "CRIAR_PRE_ALUNO")]
    public async Task<IActionResult> SubmeterParaAprovacao(int id, CancellationToken cancellationToken)
    {
        var uc = AppUserContextMapper.From(HttpContext.GetUserContext());
        var r = await _preAlunosService.SubmeterParaAprovacaoAsync(id, uc, cancellationToken);
        return r.ToActionResult(this, "Falha ao submeter pre-aluno para aprovacao.");
    }

    [HttpPatch("{id:int}/aprovar")]
    [RequirePermission("APROVAR_MATRICULA")]
    public async Task<IActionResult> Aprovar(int id, CancellationToken cancellationToken)
    {
        var uc = AppUserContextMapper.From(HttpContext.GetUserContext());
        var r = await _preAlunosService.AprovarAsync(id, uc, cancellationToken);
        return r.ToActionResult(this, "Falha ao aprovar pre-aluno.");
    }

    [HttpPatch("{id:int}/cancelar")]
    [RequirePermission("CANCELAR_PRE_ALUNO")]
    public async Task<IActionResult> Cancelar(int id, CancellationToken cancellationToken)
    {
        var uc = AppUserContextMapper.From(HttpContext.GetUserContext());
        var r = await _preAlunosService.CancelarAsync(id, uc, cancellationToken);
        return r.ToActionResult(this, "Falha ao cancelar pre-aluno.");
    }
}
