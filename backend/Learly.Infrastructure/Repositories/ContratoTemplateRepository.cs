using Learly.Domain.Entities;
using Learly.Domain.Interfaces.Repositories;
using Learly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Learly.Infrastructure.Repositories;

internal sealed class ContratoTemplateRepository(LearlyDbContext db)
    : RepositoryBase<ContratoTemplate, int>(db), IContratoTemplateRepository
{
    public async Task<IReadOnlyList<ContratoTemplate>> ListarPorEscolaAsync(
        int escolaId,
        CancellationToken cancellationToken = default)
    {
        return await Set.AsNoTracking()
            .Where(t => t.EscolaId == escolaId)
            .OrderByDescending(t => t.Versao)
            .ToListAsync(cancellationToken);
    }

    public Task<ContratoTemplate?> ObterAtivoAsync(int escolaId, CancellationToken cancellationToken = default)
    {
        return Set.AsNoTracking()
            .FirstOrDefaultAsync(t => t.EscolaId == escolaId && t.Ativo, cancellationToken);
    }

    public Task<ContratoTemplate?> ObterPorIdEEscolaAsync(int id, int escolaId, CancellationToken cancellationToken = default)
    {
        return Set.FirstOrDefaultAsync(t => t.Id == id && t.EscolaId == escolaId, cancellationToken);
    }

    public async Task<int> ProximaVersaoAsync(int escolaId, CancellationToken cancellationToken = default)
    {
        var max = await Set.AsNoTracking()
            .Where(t => t.EscolaId == escolaId)
            .MaxAsync(t => (int?)t.Versao, cancellationToken);

        return (max ?? 0) + 1;
    }
}
