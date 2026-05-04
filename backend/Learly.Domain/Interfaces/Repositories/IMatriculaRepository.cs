using Learly.Domain.Entities;

namespace Learly.Domain.Interfaces.Repositories;

public interface IMatriculaRepository : IRepository<Matricula, int>
{
    Task<Matricula?> ObterPorIdEEscolaAsync(int matriculaId, int escolaId, CancellationToken cancellationToken = default);
    Task<Matricula?> ObterRastreadaPorIdEEscolaAsync(int matriculaId, int escolaId, CancellationToken cancellationToken = default);

    Task<bool> ExisteAlunoNaEscolaAsync(int alunoId, int escolaId, CancellationToken cancellationToken = default);

    Task<bool> ExisteDuplicidadeAsync(int escolaId, int alunoId, int? turmaId, CancellationToken cancellationToken = default);

    Task<bool> ExisteMatriculaEmEsperaSemTurmaAsync(int escolaId, int alunoId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Matricula>> ListarPorEscolaComFiltrosAsync(
        int escolaId,
        string? status,
        int? alunoId,
        int? turmaId,
        CancellationToken cancellationToken = default);
}
