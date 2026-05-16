using Learly.Domain.Entities;

namespace Learly.Domain.Interfaces.Repositories;

public interface IPresencaRepository
{
    Task<IReadOnlyList<Presenca>> ListarPorAulaAsync(int escolaId, int aulaId, CancellationToken cancellationToken = default);

    Task<Presenca?> ObterRastreadaPorAulaAlunoAsync(
        int escolaId,
        int aulaId,
        int alunoId,
        CancellationToken cancellationToken = default);

    Task AdicionarAsync(Presenca presenca, CancellationToken cancellationToken = default);

    Task RemoverAsync(Presenca presenca, CancellationToken cancellationToken = default);
}
