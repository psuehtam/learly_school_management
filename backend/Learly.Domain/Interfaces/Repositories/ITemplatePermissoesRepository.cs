namespace Learly.Domain.Interfaces.Repositories;

public interface ITemplatePermissoesRepository
{
    /// <summary>
    /// Lê <c>perfis_template</c> e <c>perfil_permissoes_template</c> e retorna mapa nome do perfil → nomes das permissões.
    /// Perfis sem vínculos aparecem com lista vazia.
    /// </summary>
    Task<Dictionary<string, List<string>>> ObterPermissoesDeTemplateAsync(
        CancellationToken cancellationToken = default);

    Task<bool> PerfilTemplateExisteAsync(int perfilTemplateId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<(int Id, string Nome)>> ListarPerfisTemplateAsync(
        CancellationToken cancellationToken = default);

    Task<string?> ObterNomePerfilTemplateAsync(int perfilTemplateId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<int>> ObterPermissaoIdsDoPerfilTemplateAsync(
        int perfilTemplateId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove vínculos existentes e adiciona os novos (sem <c>SaveChanges</c>).
    /// </summary>
    Task SubstituirVinculosPermissoesAsync(
        int perfilTemplateId,
        IReadOnlyList<int> permissaoIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Propaga as permissões de um perfil-template para TODAS as escolas existentes:
    /// substitui integralmente os vínculos em <c>perfil_permissoes</c> de cada
    /// <c>perfis</c> cujo nome coincida com <paramref name="nomePerfilTemplate"/>,
    /// excluindo a escola reservada <c>SYSTEM</c>.
    /// Deve ser chamado dentro de uma transação aberta.
    /// </summary>
    Task PropagateToAllEscolasAsync(
        string nomePerfilTemplate,
        IReadOnlyList<int> permissaoIds,
        CancellationToken cancellationToken = default);
}
