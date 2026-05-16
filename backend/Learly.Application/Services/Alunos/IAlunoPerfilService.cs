using Learly.Application.Contracts.Alunos.Requests;
using Learly.Application.Contracts.Alunos.Responses;
using Learly.Application.Services.Common;

namespace Learly.Application.Services.Alunos;

public interface IAlunoPerfilService
{
    Task<IReadOnlyList<AlunoOcorrenciaResponse>> ListarOcorrenciasAsync(
        int alunoId,
        AppUserContext uc,
        CancellationToken cancellationToken = default);

    Task<(bool Ok, AlunoOcorrenciaResponse? Item, string? Erro, int StatusCode)> SalvarOcorrenciaAsync(
        int alunoId,
        int? ocorrenciaId,
        SalvarAlunoOcorrenciaRequest request,
        AppUserContext uc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AlunoDocumentoResponse>> ListarDocumentosAsync(
        int alunoId,
        AppUserContext uc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AlunoFaltaResponse>> ListarFaltasAsync(
        int alunoId,
        AppUserContext uc,
        CancellationToken cancellationToken = default);

    Task<(bool Ok, string? Erro, int StatusCode)> JustificarFaltaAsync(
        int alunoId,
        int presencaId,
        JustificarAlunoFaltaRequest request,
        AppUserContext uc,
        CancellationToken cancellationToken = default);
}
