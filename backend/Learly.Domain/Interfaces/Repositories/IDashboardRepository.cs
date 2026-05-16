using Learly.Domain.ReadModels;

namespace Learly.Domain.Interfaces.Repositories;

public interface IDashboardRepository
{
    Task<DashboardDadosEscola> ObterDadosEscolaAsync(
        int escolaId,
        DateOnly dataReferencia,
        CancellationToken cancellationToken = default);
}
