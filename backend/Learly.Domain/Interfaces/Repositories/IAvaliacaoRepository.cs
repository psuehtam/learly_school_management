using Learly.Domain.Entities;

namespace Learly.Domain.Interfaces.Repositories;

public interface IAvaliacaoRepository
{
    Task<IReadOnlyList<Avaliacao>> ListarPorTurmaAsync(int escolaId, int turmaId, CancellationToken cancellationToken = default);

    Task<Avaliacao?> ObterRastreadaAsync(
        int escolaId,
        int turmaId,
        int alunoId,
        string tipoAvaliacao,
        CancellationToken cancellationToken = default);

    Task AdicionarAsync(Avaliacao avaliacao, CancellationToken cancellationToken = default);
}
