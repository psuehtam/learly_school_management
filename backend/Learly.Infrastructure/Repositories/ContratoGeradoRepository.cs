using Learly.Domain.Entities;
using Learly.Domain.Interfaces.Repositories;
using Learly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Learly.Infrastructure.Repositories;

internal sealed class ContratoGeradoRepository(LearlyDbContext db)
    : RepositoryBase<ContratoGerado, int>(db), IContratoGeradoRepository
{
    public async Task<IReadOnlyList<ContratoGerado>> ListarPorPreAlunoAsync(
        int preAlunoId,
        int escolaId,
        CancellationToken cancellationToken = default)
    {
        return await Set.AsNoTracking()
            .Where(g => g.PreAlunoId == preAlunoId && g.EscolaId == escolaId)
            .OrderByDescending(g => g.DataGeracao)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ContratoGerado>> ListarPorEscolaAsync(
        int escolaId,
        CancellationToken cancellationToken = default)
    {
        return await Set.AsNoTracking()
            .Where(g => g.EscolaId == escolaId)
            .OrderByDescending(g => g.DataGeracao)
            .ToListAsync(cancellationToken);
    }

    public Task<ContratoGerado?> ObterPorIdEEscolaAsync(int id, int escolaId, CancellationToken cancellationToken = default)
    {
        return Set.AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == id && g.EscolaId == escolaId, cancellationToken);
    }
}
