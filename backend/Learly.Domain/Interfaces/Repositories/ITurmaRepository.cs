using Learly.Domain.Entities;
using Learly.Domain.ReadModels;

namespace Learly.Domain.Interfaces.Repositories;

public interface ITurmaRepository : IRepository<Turma, int>
{
    Task<IReadOnlyList<Turma>> ListarPorEscolaAsync(int escolaId, CancellationToken cancellationToken = default);

    Task<Turma?> ObterPorIdEEscolaAsync(int turmaId, int escolaId, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<int, TurmaResumoAgenda>> ObterResumoParaAgendaAsync(
        int escolaId,
        IReadOnlyList<int> turmaIds,
        CancellationToken cancellationToken = default);
}
