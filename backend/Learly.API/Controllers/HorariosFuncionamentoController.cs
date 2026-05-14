using Learly.API.Auth;
using Learly.API.Auth.Filters;
using Learly.API.Http;
using Learly.API.Mapping;
using Learly.Application.Contracts.HorariosFuncionamento.Requests;
using Learly.Application.Services.HorariosFuncionamento;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Learly.API.Controllers;

[ApiController]
[Route("api/horarios-funcionamento")]
[Authorize]
[SchoolUserOnly]
public sealed class HorariosFuncionamentoController : ControllerBase
{
    private readonly IHorariosFuncionamentoService _service;

    public HorariosFuncionamentoController(IHorariosFuncionamentoService service)
    {
        _service = service;
    }

    [HttpGet]
    [RequirePermission("GERENCIAR_CONFIGURACOES_SISTEMA")]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken)
    {
        var resultado = await _service.ListarAsync(
            AppUserContextMapper.From(HttpContext.GetUserContext()),
            cancellationToken);
        return resultado.ToActionResult(this);
    }

    [HttpPut]
    [RequirePermission("GERENCIAR_CONFIGURACOES_SISTEMA")]
    public async Task<IActionResult> Atualizar(
        [FromBody] AtualizarHorariosEscolaRequest body,
        CancellationToken cancellationToken)
    {
        var resultado = await _service.AtualizarAsync(
            body,
            AppUserContextMapper.From(HttpContext.GetUserContext()),
            cancellationToken);
        return resultado.ToActionResult(this);
    }
}
