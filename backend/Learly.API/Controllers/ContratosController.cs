using Learly.API.Auth;
using Learly.API.Auth.Filters;
using Learly.API.Http;
using Learly.API.Mapping;
using Learly.Application.Contracts.Contratos.Requests;
using Learly.Application.Contracts.Contratos.Responses;
using Learly.Application.Services.Contratos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Learly.API.Controllers;

/// <summary>
/// Templates de contrato e contratos gerados, com isolamento por escola (multi-tenant).
/// </summary>
[ApiController]
[Route("api/contratos")]
[Authorize]
[SchoolUserOnly]
public sealed class ContratosController : ControllerBase
{
    private readonly IContratosService _service;

    public ContratosController(IContratosService service)
    {
        _service = service;
    }

    // ──────────────── Templates ────────────────

    /// <summary>Lista todos os templates de contrato da escola.</summary>
    [HttpGet("templates")]
    [RequirePermission("VISUALIZAR_TEMPLATE_CONTRATO")]
    public async Task<ActionResult<IReadOnlyList<ContratoTemplateListItemResponse>>> ListarTemplates(CancellationToken cancellationToken)
    {
        var uc = AppUserContextMapper.From(HttpContext.GetUserContext());
        var lista = await _service.ListarTemplatesAsync(uc, cancellationToken);
        return Ok(lista);
    }

    /// <summary>Retorna o template atualmente ativo da escola.</summary>
    [HttpGet("templates/ativo")]
    [RequirePermission("VISUALIZAR_TEMPLATE_CONTRATO")]
    public async Task<ActionResult<ContratoTemplateListItemResponse>> ObterTemplateAtivo(CancellationToken cancellationToken)
    {
        var uc = AppUserContextMapper.From(HttpContext.GetUserContext());
        var template = await _service.ObterTemplateAtivoAsync(uc, cancellationToken);
        if (template is null)
            return NotFound(new ProblemDetails { Title = "Nao encontrado", Detail = "Nenhum template ativo." });
        return Ok(template);
    }

    /// <summary>Retorna um template específico pelo ID.</summary>
    [HttpGet("templates/{id:int}")]
    [RequirePermission("VISUALIZAR_TEMPLATE_CONTRATO")]
    public async Task<ActionResult<ContratoTemplateListItemResponse>> ObterTemplate(int id, CancellationToken cancellationToken)
    {
        var uc = AppUserContextMapper.From(HttpContext.GetUserContext());
        var template = await _service.ObterTemplatePorIdAsync(id, uc, cancellationToken);
        if (template is null)
            return NotFound(new ProblemDetails { Title = "Nao encontrado", Detail = "Template não encontrado." });
        return Ok(template);
    }

    /// <summary>Cria um novo template de contrato (apenas Admin).</summary>
    [HttpPost("templates")]
    [RequirePermission("CRIAR_TEMPLATE_CONTRATO")]
    public async Task<IActionResult> CriarTemplate([FromBody] CriarContratoTemplateRequest body, CancellationToken cancellationToken)
    {
        var uc = AppUserContextMapper.From(HttpContext.GetUserContext());
        var resultado = await _service.CriarTemplateAsync(body, uc, cancellationToken);
        return resultado.ToActionResult(this, "Falha ao criar template.");
    }

    /// <summary>Edita nome e conteúdo de um template existente (apenas Admin).</summary>
    [HttpPut("templates/{id:int}")]
    [RequirePermission("EDITAR_TEMPLATE_CONTRATO")]
    public async Task<IActionResult> EditarTemplate(int id, [FromBody] EditarContratoTemplateRequest body, CancellationToken cancellationToken)
    {
        var uc = AppUserContextMapper.From(HttpContext.GetUserContext());
        var resultado = await _service.EditarTemplateAsync(id, body, uc, cancellationToken);
        return resultado.ToActionResult(this, "Falha ao editar template.");
    }

    /// <summary>Ativa um template (desativa todos os outros da escola).</summary>
    [HttpPatch("templates/{id:int}/ativar")]
    [RequirePermission("EDITAR_TEMPLATE_CONTRATO")]
    public async Task<IActionResult> AtivarTemplate(int id, CancellationToken cancellationToken)
    {
        var uc = AppUserContextMapper.From(HttpContext.GetUserContext());
        var resultado = await _service.AtivarTemplateAsync(id, uc, cancellationToken);
        return resultado.ToActionResult(this, "Falha ao ativar template.");
    }

    /// <summary>Inativa um template.</summary>
    [HttpPatch("templates/{id:int}/inativar")]
    [RequirePermission("INATIVAR_TEMPLATE_CONTRATO")]
    public async Task<IActionResult> InativarTemplate(int id, CancellationToken cancellationToken)
    {
        var uc = AppUserContextMapper.From(HttpContext.GetUserContext());
        var resultado = await _service.InativarTemplateAsync(id, uc, cancellationToken);
        return resultado.ToActionResult(this, "Falha ao inativar template.");
    }

    /// <summary>Lista todas as variáveis disponíveis para uso nos templates.</summary>
    [HttpGet("templates/variaveis")]
    [RequirePermission("VISUALIZAR_TEMPLATE_CONTRATO")]
    public async Task<IActionResult> ListarVariaveis()
    {
        var lista = await _service.ListarVariaveisAsync();
        var resultado = lista.Select(v => new { variavel = v.Variavel, descricao = v.Descricao });
        return Ok(resultado);
    }

    // ──────────────── Contratos gerados ────────────────

    /// <summary>Lista todos os contratos gerados da escola.</summary>
    [HttpGet("gerados")]
    [RequirePermission("VISUALIZAR_CONTRATO")]
    public async Task<ActionResult<IReadOnlyList<ContratoGeradoResponse>>> ListarGerados(CancellationToken cancellationToken)
    {
        var uc = AppUserContextMapper.From(HttpContext.GetUserContext());
        var lista = await _service.ListarContratosGeradosAsync(uc, cancellationToken);
        return Ok(lista);
    }

    /// <summary>Lista contratos gerados para um pré-aluno específico.</summary>
    [HttpGet("gerados/pre-aluno/{preAlunoId:int}")]
    [RequirePermission("VISUALIZAR_CONTRATO")]
    public async Task<ActionResult<IReadOnlyList<ContratoGeradoResponse>>> ListarGeradosPorPreAluno(int preAlunoId, CancellationToken cancellationToken)
    {
        var uc = AppUserContextMapper.From(HttpContext.GetUserContext());
        var lista = await _service.ListarContratosGeradosPorPreAlunoAsync(preAlunoId, uc, cancellationToken);
        return Ok(lista);
    }

    /// <summary>
    /// Gera um contrato substituindo todas as variáveis do template pelos dados reais do pré-aluno.
    /// Persiste o contrato gerado e retorna o HTML final.
    /// </summary>
    [HttpPost("gerar")]
    [RequirePermission("GERAR_CONTRATO")]
    public async Task<IActionResult> GerarContrato([FromBody] GerarContratoRequest body, CancellationToken cancellationToken)
    {
        var uc = AppUserContextMapper.From(HttpContext.GetUserContext());
        var resultado = await _service.GerarContratoAsync(body, uc, cancellationToken);
        return resultado.ToActionResult(this);
    }
}
