using Learly.Application.Contracts.Turmas;
using Learly.Application.Contracts.Turmas.Requests;
using Learly.Application.Contracts.Turmas.Responses;
using Learly.Application.Services.Common;

namespace Learly.Application.Services.Turmas;

public interface ITurmasService
{
    Task<TurmasListagemResultado> ListarAsync(string? status, AppUserContext uc, CancellationToken cancellationToken = default);

    Task<TurmaDetalheResultado> ObterPorIdAsync(int id, AppUserContext uc, CancellationToken cancellationToken = default);

    Task<TurmaOperacaoResultado> CriarAsync(CriarTurmaRequest request, AppUserContext uc, CancellationToken cancellationToken = default);

    Task<TurmaOperacaoResultado> AtualizarAsync(int id, AtualizarTurmaRequest request, AppUserContext uc, CancellationToken cancellationToken = default);

    Task<TurmaOperacaoResultado> AtivarAsync(int id, AtivarTurmaRequest request, AppUserContext uc, CancellationToken cancellationToken = default);

    Task<TurmaOperacaoResultado> ConcluirAsync(int id, AppUserContext uc, CancellationToken cancellationToken = default);

    Task<TurmaOperacaoResultado> InativarAsync(int id, AppUserContext uc, CancellationToken cancellationToken = default);

    /// <summary>Recalcula aulas agendadas e data de término das turmas em andamento (ex.: após feriado no calendário).</summary>
    Task RecalcularTurmasEmAndamentoDaEscolaAsync(int escolaId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AvaliacaoTurmaResponse>> ListarAvaliacoesAsync(
        int turmaId,
        AppUserContext uc,
        CancellationToken cancellationToken = default);

    Task<TurmaOperacaoResultado> SalvarAvaliacoesAsync(
        int turmaId,
        SalvarAvaliacoesTurmaRequest request,
        AppUserContext uc,
        CancellationToken cancellationToken = default);
}
