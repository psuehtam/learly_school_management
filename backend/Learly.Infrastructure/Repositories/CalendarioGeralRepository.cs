using Learly.Domain.Entities;
using Learly.Domain.Interfaces.Repositories;
using Learly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Learly.Infrastructure.Repositories;

internal sealed class CalendarioGeralRepository(LearlyDbContext db)
    : RepositoryBase<CalendarioGeral, int>(db), ICalendarioGeralRepository
{
    public async Task<IReadOnlyList<CalendarioGeral>> ListarPorPeriodoAsync(
        int escolaId,
        DateOnly dataInicio,
        DateOnly dataFim,
        CancellationToken cancellationToken = default)
    {
        return await Db.CalendariosGerais
            .AsNoTracking()
            .Where(c => c.EscolaId == escolaId && c.DataEvento >= dataInicio && c.DataEvento <= dataFim)
            .OrderBy(c => c.DataEvento)
            .ToListAsync(cancellationToken);
    }

    public Task<CalendarioGeral?> ObterPorIdEEscolaAsync(
        int id,
        int escolaId,
        CancellationToken cancellationToken = default)
    {
        return Db.CalendariosGerais
            .FirstOrDefaultAsync(c => c.Id == id && c.EscolaId == escolaId, cancellationToken);
    }

    public Task<CalendarioGeral?> ObterPorDataAsync(
        int escolaId,
        DateOnly dataEvento,
        CancellationToken cancellationToken = default)
    {
        return Db.CalendariosGerais
            .FirstOrDefaultAsync(c => c.EscolaId == escolaId && c.DataEvento == dataEvento, cancellationToken);
    }

    public Task<bool> DiaSuspendeAulaAsync(
        int escolaId,
        DateOnly dataEvento,
        CancellationToken cancellationToken = default)
    {
        return Db.CalendariosGerais
            .AsNoTracking()
            .AnyAsync(c => c.EscolaId == escolaId && c.DataEvento == dataEvento && c.SuspendeAula, cancellationToken);
    }
}
