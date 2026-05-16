using Learly.Domain.Entities;
using Learly.Domain.Interfaces.Repositories;
using Learly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Learly.Infrastructure.Repositories;

internal sealed class AvaliacaoRepository(LearlyDbContext db) : IAvaliacaoRepository
{
    public async Task<IReadOnlyList<Avaliacao>> ListarPorTurmaAsync(
        int escolaId,
        int turmaId,
        CancellationToken cancellationToken = default) =>
        await db.Avaliacoes.AsNoTracking()
            .Where(a => a.EscolaId == escolaId && a.TurmaId == turmaId)
            .OrderBy(a => a.AlunoId)
            .ThenBy(a => a.TipoAvaliacao)
            .ToListAsync(cancellationToken);

    public Task<Avaliacao?> ObterRastreadaAsync(
        int escolaId,
        int turmaId,
        int alunoId,
        string tipoAvaliacao,
        CancellationToken cancellationToken = default) =>
        db.Avaliacoes.FirstOrDefaultAsync(
            a => a.EscolaId == escolaId
                 && a.TurmaId == turmaId
                 && a.AlunoId == alunoId
                 && a.TipoAvaliacao == tipoAvaliacao,
            cancellationToken);

    public async Task AdicionarAsync(Avaliacao avaliacao, CancellationToken cancellationToken = default)
    {
        await db.Avaliacoes.AddAsync(avaliacao, cancellationToken);
    }
}
