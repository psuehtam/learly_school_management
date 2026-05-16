using Learly.Domain.Entities;
using Learly.Domain.Interfaces.Repositories;
using Learly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Learly.Infrastructure.Repositories;

internal sealed class HomeworkRepository(LearlyDbContext db) : IHomeworkRepository
{
    public async Task<IReadOnlyList<Homework>> ListarPorAulaAsync(
        int escolaId,
        int aulaId,
        CancellationToken cancellationToken = default) =>
        await db.Homeworks.AsNoTracking()
            .Where(h => h.EscolaId == escolaId && h.AulaId == aulaId)
            .OrderBy(h => h.AlunoId)
            .ToListAsync(cancellationToken);

    public Task<Homework?> ObterRastreadoPorAulaAlunoAsync(
        int escolaId,
        int aulaId,
        int alunoId,
        CancellationToken cancellationToken = default) =>
        db.Homeworks.FirstOrDefaultAsync(
            h => h.EscolaId == escolaId && h.AulaId == aulaId && h.AlunoId == alunoId,
            cancellationToken);

    public async Task AdicionarAsync(Homework homework, CancellationToken cancellationToken = default)
    {
        await db.Homeworks.AddAsync(homework, cancellationToken);
    }

    public void Remover(Homework homework) => db.Homeworks.Remove(homework);
}
