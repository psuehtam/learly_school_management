using Learly.Domain.Entities;
using Learly.Domain.Interfaces.Repositories;
using Learly.Domain.ReadModels;
using Learly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Learly.Infrastructure.Repositories;

internal sealed class TurmaRepository(LearlyDbContext db) : RepositoryBase<Turma, int>(db), ITurmaRepository
{
    public async Task<IReadOnlyDictionary<int, TurmaResumoAgenda>> ObterResumoParaAgendaAsync(
        int escolaId,
        IReadOnlyList<int> turmaIds,
        CancellationToken cancellationToken = default)
    {
        if (turmaIds.Count == 0)
        {
            return new Dictionary<int, TurmaResumoAgenda>();
        }

        var distinct = turmaIds.Distinct().ToList();
        var rows = await (
            from t in Db.Turmas.AsNoTracking()
            join l in Db.Livros.AsNoTracking() on t.LivroId equals l.Id into livroJoin
            from l in livroJoin.DefaultIfEmpty()
            where t.EscolaId == escolaId && distinct.Contains(t.Id)
            select new { t.Id, t.Nome, LivroNome = l != null ? l.Nome : "" }
        ).ToListAsync(cancellationToken);

        return rows.ToDictionary(r => r.Id, r => new TurmaResumoAgenda(r.Nome, r.LivroNome));
    }

    public async Task<IReadOnlyList<Turma>> ListarPorEscolaAsync(int escolaId, CancellationToken cancellationToken = default)
    {
        return await Db.Turmas
            .AsNoTracking()
            .Where(t => t.EscolaId == escolaId)
            .OrderBy(t => t.Nome)
            .ToListAsync(cancellationToken);
    }

    public async Task<Turma?> ObterPorIdEEscolaAsync(int turmaId, int escolaId, CancellationToken cancellationToken = default)
    {
        return await Db.Turmas.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == turmaId && t.EscolaId == escolaId, cancellationToken);
    }
}
