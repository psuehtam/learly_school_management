using Learly.Domain.Entities;
using Learly.Domain.ReadModels;

namespace Learly.Domain.Interfaces.Repositories;

public interface IPreAlunoRepository : IRepository<PreAluno, int>
{
    Task<IReadOnlyList<PreAlunoListagemItem>> ListarPorEscolaAsync(
        int escolaId,
        string? filtroStatus,
        CancellationToken cancellationToken = default);

    Task<PreAlunoDetalheItem?> ObterDetalheAsync(int id, int escolaId, CancellationToken cancellationToken = default);

    Task<bool> ExisteLivroAtivoNaEscolaAsync(int escolaId, int livroId, CancellationToken cancellationToken = default);

    Task<PreAluno?> ObterPorIdEEscolaRastreadoAsync(int id, int escolaId, CancellationToken cancellationToken = default);
}
