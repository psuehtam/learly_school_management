using Learly.Application.Contracts.Contratos;
using Learly.Application.Contracts.Contratos.Requests;
using Learly.Application.Contracts.Contratos.Responses;
using Learly.Application.Services.Common;

namespace Learly.Application.Services.Contratos;

public interface IContratosService
{
    // Templates
    Task<IReadOnlyList<ContratoTemplateListItemResponse>> ListarTemplatesAsync(AppUserContext uc, CancellationToken cancellationToken = default);
    Task<ContratoTemplateListItemResponse?> ObterTemplateAtivoAsync(AppUserContext uc, CancellationToken cancellationToken = default);
    Task<ContratoTemplateListItemResponse?> ObterTemplatePorIdAsync(int id, AppUserContext uc, CancellationToken cancellationToken = default);
    Task<ContratoTemplateOperacaoResultado> CriarTemplateAsync(CriarContratoTemplateRequest request, AppUserContext uc, CancellationToken cancellationToken = default);
    Task<ContratoTemplateOperacaoResultado> EditarTemplateAsync(int id, EditarContratoTemplateRequest request, AppUserContext uc, CancellationToken cancellationToken = default);
    Task<ContratoTemplateOperacaoResultado> AtivarTemplateAsync(int id, AppUserContext uc, CancellationToken cancellationToken = default);
    Task<ContratoTemplateOperacaoResultado> InativarTemplateAsync(int id, AppUserContext uc, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<(string Variavel, string Descricao)>> ListarVariaveisAsync();

    // Contratos gerados
    Task<IReadOnlyList<ContratoGeradoResponse>> ListarContratosGeradosAsync(AppUserContext uc, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ContratoGeradoResponse>> ListarContratosGeradosPorPreAlunoAsync(int preAlunoId, AppUserContext uc, CancellationToken cancellationToken = default);
    Task<ContratoGeradoOperacaoResultado> GerarContratoAsync(GerarContratoRequest request, AppUserContext uc, CancellationToken cancellationToken = default);
}
