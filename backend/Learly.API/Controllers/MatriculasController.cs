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
    [RequirePermission("VISUALIZAR_MATRICULA")]
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
