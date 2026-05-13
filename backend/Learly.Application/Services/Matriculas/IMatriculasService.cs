using Learly.Application.Contracts.Matriculas;
using Learly.Application.Contracts.Matriculas.Requests;
using Learly.Application.Services.Common;

namespace Learly.Application.Services.Matriculas;

public interface IMatriculasService
{
    Task<MatriculaListagemResultado> ListarAsync(
        ListarMatriculasQuery filtro,
        AppUserContext uc,
        CancellationToken cancellationToken = default);

    Task<MatriculaCriacaoResultado> CriarAsync(
        CriarMatriculaRequest request,
        AppUserContext uc,
        CancellationToken cancellationToken = default);

    Task<MatriculaOperacaoResultado> CancelarAsync(
        int id,
        AppUserContext uc,
        CancellationToken cancellationToken = default);

    Task<MatriculaOperacaoResultado> VincularTurmaAsync(
        int matriculaId,
        VincularTurmaMatriculaRequest request,
        AppUserContext uc,
        CancellationToken cancellationToken = default);
}
