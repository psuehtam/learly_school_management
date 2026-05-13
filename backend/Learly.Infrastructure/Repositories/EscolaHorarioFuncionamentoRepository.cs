using Learly.Domain.Entities;
using Learly.Domain.Interfaces.Repositories;
using Learly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Learly.Infrastructure.Repositories;

internal sealed class EscolaHorarioFuncionamentoRepository(LearlyDbContext db) : IEscolaHorarioFuncionamentoRepository
{
    public Task<bool> PossuiConfiguracaoAsync(int escolaId, CancellationToken cancellationToken = default)
    {
        return db.Set<EscolaHorarioFuncionamento>()
            .AsNoTracking()
            .AnyAsync(h => h.EscolaId == escolaId, cancellationToken);
    }

    public Task<EscolaHorarioFuncionamento?> ObterPorDiaSemanaAsync(
        int escolaId,
        int diaSemana,
        CancellationToken cancellationToken = default)
    {
        return db.Set<EscolaHorarioFuncionamento>()
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.EscolaId == escolaId && h.DiaSemana == diaSemana, cancellationToken);
    }

    public Task<List<EscolaHorarioFuncionamento>> ListarRastreadosPorEscolaAsync(
        int escolaId,
        CancellationToken cancellationToken = default)
    {
        return db.Set<EscolaHorarioFuncionamento>()
            .Where(h => h.EscolaId == escolaId)
            .ToListAsync(cancellationToken);
    }

    public Task<List<EscolaHorarioFuncionamento>> ListarPorEscolaAsync(
        int escolaId,
        CancellationToken cancellationToken = default)
    {
        return db.Set<EscolaHorarioFuncionamento>()
            .AsNoTracking()
            .Where(h => h.EscolaId == escolaId)
            .OrderBy(h => h.DiaSemana)
            .ToListAsync(cancellationToken);
    }

    public async Task AdicionarAsync(EscolaHorarioFuncionamento horario, CancellationToken cancellationToken = default)
    {
        await db.Set<EscolaHorarioFuncionamento>().AddAsync(horario, cancellationToken);
    }
}
