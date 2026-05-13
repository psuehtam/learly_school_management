using Learly.API.Auth;
using Learly.API.Auth.Filters;
using Learly.API.Http;
using Learly.API.Mapping;
using Learly.Application.Contracts.Livros.Requests;
using Learly.Application.Services.Livros;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Learly.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[SchoolUserOnly]
public sealed class LivrosController : ControllerBase
{
    private readonly ILivrosEscolaService _livros;

    public LivrosController(ILivrosEscolaService livros)
    {
        _livros = livros;
    }

    [HttpGet]
    [RequirePermission("VISUALIZAR_LIVRO")]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken)
    {
        var resultado = await _livros.ListarAsync(AppUserContextMapper.From(HttpContext.GetUserContext()), cancellationToken);
        return resultado.ToActionResult(this);
    }

    [HttpGet("{id:int}")]
    [RequirePermission("VISUALIZAR_LIVRO")]
    public async Task<IActionResult> Obter(int id, CancellationToken cancellationToken)
    {
        var resultado = await _livros.ObterPorIdAsync(id, AppUserContextMapper.From(HttpContext.GetUserContext()), cancellationToken);
        return resultado.ToActionResult(this);
    }

    [HttpPost]
    [RequirePermission("CRIAR_LIVRO")]
    public async Task<IActionResult> Criar([FromBody] CriarLivroEscolaRequest body, CancellationToken cancellationToken)
    {
        var resultado = await _livros.CriarAsync(body, AppUserContextMapper.From(HttpContext.GetUserContext()), cancellationToken);
        return resultado.ToActionResult(this);
    }

    [HttpPatch("{id:int}")]
    [RequirePermission("EDITAR_LIVRO")]
    public async Task<IActionResult> Atualizar(
        int id,
        [FromBody] AtualizarLivroEscolaRequest body,
        CancellationToken cancellationToken)
    {
        var resultado = await _livros.AtualizarAsync(id, body, AppUserContextMapper.From(HttpContext.GetUserContext()), cancellationToken);
        return resultado.ToActionResult(this);
    }
}
