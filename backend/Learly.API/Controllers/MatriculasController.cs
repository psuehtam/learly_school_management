using Learly.API.Auth;
using Learly.API.Auth.Filters;
using Learly.API.Http;
using Learly.API.Mapping;
using Learly.Application.Contracts.Matriculas.Requests;
using Learly.Application.Services.Matriculas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Learly.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[SchoolUserOnly]
public sealed class MatriculasController : ControllerBase
{
    private readonly IMatriculasService _matriculasService;

    public MatriculasController(IMatriculasService matriculasService)
    {
        _matriculasService = matriculasService;
    }

    [HttpGet]
    /// <summary>Secretaria/admin: todas as matrículas. Professor: somente alunos da própria turma (filtro turmaId).</summary>
    [RequirePermission("VISUALIZAR_MATRICULA", "VISUALIZAR_TURMA")]
    public async Task<IActionResult> Listar([FromQuery] ListarMatriculasQuery? filtro, CancellationToken cancellationToken)
    {
        var uc = AppUserContextMapper.From(HttpContext.GetUserContext());
        var resultado = await _matriculasService.ListarAsync(filtro ?? new ListarMatriculasQuery(), uc, cancellationToken);
        return resultado.ToActionResult(this);
    }

    [HttpPost]
    [RequirePermission("CRIAR_MATRICULA")]
    public async Task<IActionResult> Criar([FromBody] CriarMatriculaRequest body, CancellationToken cancellationToken)
    {
        var uc = AppUserContextMapper.From(HttpContext.GetUserContext());
        var resultado = await _matriculasService.CriarAsync(body, uc, cancellationToken);
        return resultado.ToActionResult(this);
    }

    [HttpPatch("{id:int}/cancelar")]
    [RequirePermission("CANCELAR_MATRICULA")]
    public async Task<IActionResult> Cancelar(int id, CancellationToken cancellationToken)
    {
        var uc = AppUserContextMapper.From(HttpContext.GetUserContext());
        var resultado = await _matriculasService.CancelarAsync(id, uc, cancellationToken);
        return resultado.ToActionResult(this, "Falha ao cancelar matricula.");
    }

    [HttpPatch("{id:int}/remover-da-turma")]
    [RequirePermission("EDITAR_MATRICULA")]
    public async Task<IActionResult> RemoverDaTurma(int id, CancellationToken cancellationToken)
    {
        var uc = AppUserContextMapper.From(HttpContext.GetUserContext());
        var resultado = await _matriculasService.RemoverDaTurmaAsync(id, uc, cancellationToken);
        return resultado.ToActionResult(this, "Falha ao remover aluno da turma.");
    }

    [HttpPatch("{id:int}/vincular-turma")]
    [RequirePermission("EDITAR_MATRICULA")]
    public async Task<IActionResult> VincularTurma(
        int id,
        [FromBody] VincularTurmaMatriculaRequest body,
        CancellationToken cancellationToken)
    {
        var uc = AppUserContextMapper.From(HttpContext.GetUserContext());
        var resultado = await _matriculasService.VincularTurmaAsync(id, body, uc, cancellationToken);
        return resultado.ToActionResult(this, "Falha ao vincular turma na matricula.");
    }
}
