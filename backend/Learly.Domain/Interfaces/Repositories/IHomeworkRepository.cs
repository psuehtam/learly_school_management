using Learly.Domain.Entities;

namespace Learly.Domain.Interfaces.Repositories;

public interface IHomeworkRepository
{
    Task<IReadOnlyList<Homework>> ListarPorAulaAsync(int escolaId, int aulaId, CancellationToken cancellationToken = default);

    Task<Homework?> ObterRastreadoPorAulaAlunoAsync(
        int escolaId,
        int aulaId,
        int alunoId,
        CancellationToken cancellationToken = default);

    Task AdicionarAsync(Homework homework, CancellationToken cancellationToken = default);

    void Remover(Homework homework);
}
