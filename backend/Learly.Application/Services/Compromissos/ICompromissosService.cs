using Learly.Application.Contracts.Compromissos.Requests;
using Learly.Application.Contracts.Compromissos.Responses;
using Learly.Application.Services.Common;

namespace Learly.Application.Services.Compromissos;

public interface ICompromissosService
{
    Task<IReadOnlyList<CompromissoResponse>> ListarMeusAsync(AppUserContext uc, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CompromissoResponse>> ListarAgendaGlobalAsync(
        DateOnly data,
        int? usuarioId,
        AppUserContext uc,
        CancellationToken cancellationToken = default);

    Task<(bool Success, CompromissoResponse? Item, string? Error, int StatusCode)> CriarAsync(
        CriarCompromissoRequest request,
        AppUserContext uc,
        CancellationToken cancellationToken = default);

    Task<(bool Success, CompromissoResponse? Item, string? Error, int StatusCode)> EditarAsync(
        int id,
        EditarCompromissoRequest request,
        AppUserContext uc,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string? Error, int StatusCode)> CancelarAsync(
        int id,
        string motivo,
        AppUserContext uc,
        CancellationToken cancellationToken = default);
}
