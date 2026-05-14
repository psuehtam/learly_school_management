using Learly.Domain.Entities;
using Learly.Domain.ReadModels;

namespace Learly.Domain.Interfaces.Repositories;

public interface IAulaRepository : IRepository<Aula, int>
{
    Task<IReadOnlyList<Aula>> ListarPorTurmaAsync(int turmaId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Aula>> ListarPorEscolaEFiltroProfessorAsync(
        int escolaId,
        int? filtrarProfessorId,
        CancellationToken cancellationToken = default);

    Task<Aula?> ObterSemRastreioPorIdEEscolaAsync(
        int id,
        int escolaId,
        int? filtrarProfessorId,
        CancellationToken cancellationToken = default);

    Task<Aula?> ObterRastreadaPorIdEEscolaAsync(
        int id,
        int escolaId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Para cada aula de reposição, obtém aluno e aula original a partir de <c>presencas.reposicao_de_presenca_id</c>.
    /// </summary>
    Task<IReadOnlyDictionary<int, AulaReposicaoAgendaContexto>> ObterContextoReposicaoPorAulaIdsAsync(
        int escolaId,
        IReadOnlyList<int> aulaIdsReposicao,
        CancellationToken cancellationToken = default);
}
