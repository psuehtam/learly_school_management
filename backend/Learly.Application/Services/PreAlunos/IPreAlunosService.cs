using Learly.Application.Contracts.PreAlunos;
using Learly.Application.Contracts.PreAlunos.Requests;
using Learly.Application.Services.Common;

namespace Learly.Application.Services.PreAlunos;

public interface IPreAlunosService
{
    Task<PreAlunosCatalogoLivrosResultado> ListarCatalogoLivrosInteresseAsync(
        AppUserContext uc,
        CancellationToken cancellationToken = default);

    Task<PreAlunosListagemResultado> ListarAsync(
        ListarPreAlunosQuery query,
        AppUserContext uc,
        CancellationToken cancellationToken = default);

    Task<PreAlunoDetalheResultado> ObterPorIdAsync(
        int id,
        AppUserContext uc,
        CancellationToken cancellationToken = default);

    Task<PreAlunoCriacaoResultado> CriarAsync(
        CriarPreAlunoRequest request,
        AppUserContext uc,
        CancellationToken cancellationToken = default);

    Task<PreAlunoOperacaoResultado> SubmeterParaAprovacaoAsync(
        int id,
        AppUserContext uc,
        CancellationToken cancellationToken = default);

    Task<PreAlunoOperacaoResultado> AprovarAsync(
        int id,
        AppUserContext uc,
        CancellationToken cancellationToken = default);

    Task<PreAlunoOperacaoResultado> CancelarAsync(
        int id,
        AppUserContext uc,
        CancellationToken cancellationToken = default);
}
