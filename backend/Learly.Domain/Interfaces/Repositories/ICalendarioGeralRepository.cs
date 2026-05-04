using Learly.Domain.Entities;

namespace Learly.Domain.Interfaces.Repositories;

public interface ICalendarioGeralRepository : IRepository<CalendarioGeral, int>
{
    Task<IReadOnlyList<CalendarioGeral>> ListarPorPeriodoAsync(
        int escolaId,
        DateOnly dataInicio,
        DateOnly dataFim,
        CancellationToken cancellationToken = default);

    Task<CalendarioGeral?> ObterPorIdEEscolaAsync(
        int id,
        int escolaId,
        CancellationToken cancellationToken = default);

    Task<CalendarioGeral?> ObterPorDataAsync(
        int escolaId,
        DateOnly dataEvento,
        CancellationToken cancellationToken = default);

    Task<bool> DiaSuspendeAulaAsync(
        int escolaId,
        DateOnly dataEvento,
        CancellationToken cancellationToken = default);
}
