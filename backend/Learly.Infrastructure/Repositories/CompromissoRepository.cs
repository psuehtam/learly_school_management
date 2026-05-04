using Learly.Domain.Entities;
using Learly.Domain.Interfaces.Repositories;
using Learly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Learly.Infrastructure.Repositories;

internal sealed class CompromissoRepository(LearlyDbContext db)
    : RepositoryBase<Compromisso, int>(db), ICompromissoRepository
{
    public async Task<IReadOnlyList<Compromisso>> ListarPorEscolaEUsuarioAsync(
        int escolaId,
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        var idsComoParticipante = Db.CompromissosParticipantes
            .Where(p => p.UsuarioId == usuarioId)
            .Select(p => p.CompromissoId);

        return await Db.Compromissos
            .AsNoTracking()
            .Where(c =>
                c.EscolaId == escolaId
                && (c.UsuarioId == usuarioId || idsComoParticipante.Contains(c.Id)))
            .OrderBy(c => c.DataInicio)
            .ToListAsync(cancellationToken);
    }

    public Task<Compromisso?> ObterPorIdEEscolaAsync(
        int compromissoId,
        int escolaId,
        CancellationToken cancellationToken = default)
    {
        return Db.Compromissos.FirstOrDefaultAsync(c => c.Id == compromissoId && c.EscolaId == escolaId, cancellationToken);
    }

    public async Task<IReadOnlyList<Compromisso>> ListarAgendaGlobalAsync(
        int escolaId,
        DateOnly data,
        int? usuarioId,
        CancellationToken cancellationToken = default)
    {
        var inicio = data.ToDateTime(TimeOnly.MinValue);
        var fim = data.ToDateTime(new TimeOnly(23, 59, 59));

        var query = Db.Compromissos
            .AsNoTracking()
            .Where(c =>
                c.EscolaId == escolaId
                && c.DataInicio <= fim
                && c.DataFim >= inicio);

        if (usuarioId.HasValue)
        {
            var uid = usuarioId.Value;
            var idsComoParticipante = Db.CompromissosParticipantes
                .Where(p => p.UsuarioId == uid)
                .Select(p => p.CompromissoId);

            query = query.Where(c => c.UsuarioId == uid || idsComoParticipante.Contains(c.Id));
        }

        return await query.OrderBy(c => c.DataInicio).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<int>> ListarParticipantesIdsAsync(
        int compromissoId,
        CancellationToken cancellationToken = default)
    {
        return await Db.CompromissosParticipantes
            .AsNoTracking()
            .Where(p => p.CompromissoId == compromissoId)
            .Select(p => p.UsuarioId)
            .ToListAsync(cancellationToken);
    }

    public async Task DefinirParticipantesAsync(
        int compromissoId,
        IReadOnlyCollection<int> usuarioIds,
        CancellationToken cancellationToken = default)
    {
        var existentes = await Db.CompromissosParticipantes
            .Where(p => p.CompromissoId == compromissoId)
            .ToListAsync(cancellationToken);

        Db.CompromissosParticipantes.RemoveRange(existentes);

        var novos = usuarioIds
            .Distinct()
            .Select(uid => new CompromissoParticipante
            {
                CompromissoId = compromissoId,
                UsuarioId = uid,
                Confirmacao = "Pendente"
            });

        await Db.CompromissosParticipantes.AddRangeAsync(novos, cancellationToken);
    }

    public Task<bool> ExisteConflitoCompromissoAsync(
        int escolaId,
        IReadOnlyCollection<int> usuarioIds,
        DateTime dataInicio,
        DateTime dataFim,
        int? compromissoIgnoradoId,
        CancellationToken cancellationToken = default)
    {
        var usuarios = usuarioIds.Distinct().ToList();
        if (usuarios.Count == 0)
            return Task.FromResult(false);

        var idsComoParticipante = Db.CompromissosParticipantes
            .Where(p => usuarios.Contains(p.UsuarioId))
            .Select(p => p.CompromissoId);

        var query = Db.Compromissos.AsNoTracking().Where(c =>
            c.EscolaId == escolaId
            && c.Status != "Cancelado"
            && (usuarios.Contains(c.UsuarioId) || idsComoParticipante.Contains(c.Id))
            && c.DataInicio < dataFim
            && c.DataFim > dataInicio);

        if (compromissoIgnoradoId.HasValue)
            query = query.Where(c => c.Id != compromissoIgnoradoId.Value);

        return query.AnyAsync(cancellationToken);
    }

    public Task<bool> ExisteConflitoAulaProfessorAsync(
        int escolaId,
        IReadOnlyCollection<int> usuarioIds,
        DateTime dataInicio,
        DateTime dataFim,
        CancellationToken cancellationToken = default)
    {
        var usuarios = usuarioIds.Distinct().ToList();
        if (usuarios.Count == 0)
            return Task.FromResult(false);

        var data = DateOnly.FromDateTime(dataInicio);
        var inicio = TimeOnly.FromDateTime(dataInicio);
        var fim = TimeOnly.FromDateTime(dataFim);

        return Db.Aulas
            .AsNoTracking()
            .AnyAsync(a =>
                a.EscolaId == escolaId
                && usuarios.Contains(a.ProfessorId)
                && a.Status != Aula.Estados.Cancelada
                && a.DataAula == data
                && a.HorarioInicio < fim
                && a.HorarioFim > inicio,
                cancellationToken);
    }
}
