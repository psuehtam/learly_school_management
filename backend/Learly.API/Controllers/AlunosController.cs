using Learly.API.Auth;
using Learly.API.Auth.Filters;
using Learly.API.Http;
using Learly.API.Mapping;
using Learly.Application.Contracts.Alunos.Requests;
using Learly.Application.Services.Alunos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Learly.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[SchoolUserOnly]
public sealed class AlunosController : ControllerBase
{
    private readonly IAlunosService _alunosService;

    public AlunosController(IAlunosService alunosService)
    {
        _alunosService = alunosService;
    }

    [HttpPost]
    [RequirePermission("CRIAR_ALUNO")]
    public async Task<IActionResult> Criar([FromBody] CriarAlunoRequest body, CancellationToken cancellationToken)
    {
        var uc = AppUserContextMapper.From(HttpContext.GetUserContext());
        var resultado = await _alunosService.CriarAlunoAsync(body, uc, cancellationToken);
        return resultado.ToActionResult(this);
    }
}
