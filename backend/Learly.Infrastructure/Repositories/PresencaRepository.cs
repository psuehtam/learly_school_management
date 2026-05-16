using Learly.Domain.Entities;
using Learly.Domain.Interfaces.Repositories;
using Learly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Learly.Infrastructure.Repositories;

internal sealed class PresencaRepository(LearlyDbContext db) : IPresencaRepository
{
    public async Task<IReadOnlyList<Presenca>> ListarPorAulaAsync(
        int escolaId,
        int aulaId,
        CancellationToken cancellationToken = default) =>
        await db.Presencas.AsNoTracking()
            .Where(p => p.EscolaId == escolaId && p.AulaId == aulaId)
            .OrderBy(p => p.AlunoId)
            .ToListAsync(cancellationToken);

    public Task<Presenca?> ObterRastreadaPorAulaAlunoAsync(
        int escolaId,
        int aulaId,
        int alunoId,
        CancellationToken cancellationToken = default) =>
        db.Presencas.FirstOrDefaultAsync(
            p => p.EscolaId == escolaId && p.AulaId == aulaId && p.AlunoId == alunoId,
            cancellationToken);

    public async Task AdicionarAsync(Presenca presenca, CancellationToken cancellationToken = default)
    {
        await db.Presencas.AddAsync(presenca, cancellationToken);
    }

    public Task RemoverAsync(Presenca presenca, CancellationToken cancellationToken = default)
    {
        db.Presencas.Remove(presenca);
        return Task.CompletedTask;
    }
}
