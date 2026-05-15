using Learly.Domain.Entities;

namespace Learly.Domain.Interfaces.Repositories;

public interface IContratoGeradoRepository : IRepository<ContratoGerado, int>
{
    Task<IReadOnlyList<ContratoGerado>> ListarPorPreAlunoAsync(int preAlunoId, int escolaId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ContratoGerado>> ListarPorEscolaAsync(int escolaId, CancellationToken cancellationToken = default);
    Task<ContratoGerado?> ObterPorIdEEscolaAsync(int id, int escolaId, CancellationToken cancellationToken = default);
}
