using Learly.Application.Contracts.Calendario.Requests;
using Learly.Application.Contracts.Calendario.Responses;
using Learly.Application.Services.Common;

namespace Learly.Application.Services.Calendario;

public interface ICalendarioService
{
    Task<IReadOnlyList<EventoCalendarioResponse>> ListarPorMesAsync(
        int mes,
        int ano,
        AppUserContext uc,
        CancellationToken cancellationToken = default);

    Task<(bool Success, EventoCalendarioResponse? Evento, string? Error, int StatusCode)> CriarAsync(
        CriarEventoCalendarioRequest request,
        AppUserContext uc,
        CancellationToken cancellationToken = default);

    Task<(bool Success, EventoCalendarioResponse? Evento, string? Error, int StatusCode)> EditarAsync(
        int id,
        EditarEventoCalendarioRequest request,
        AppUserContext uc,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string? Error, int StatusCode)> ExcluirAsync(
        int id,
        AppUserContext uc,
        CancellationToken cancellationToken = default);
}
