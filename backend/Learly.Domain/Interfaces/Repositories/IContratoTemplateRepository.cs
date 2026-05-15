using Learly.Domain.Entities;

namespace Learly.Domain.Interfaces.Repositories;

public interface IContratoTemplateRepository : IRepository<ContratoTemplate, int>
{
    Task<IReadOnlyList<ContratoTemplate>> ListarPorEscolaAsync(int escolaId, CancellationToken cancellationToken = default);
    Task<ContratoTemplate?> ObterAtivoAsync(int escolaId, CancellationToken cancellationToken = default);
    Task<ContratoTemplate?> ObterPorIdEEscolaAsync(int id, int escolaId, CancellationToken cancellationToken = default);
    Task<int> ProximaVersaoAsync(int escolaId, CancellationToken cancellationToken = default);
}
