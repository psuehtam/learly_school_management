using Learly.Application.Contracts.Alunos;
using Learly.Application.Contracts.Alunos.Requests;
using Learly.Application.Contracts.Alunos.Responses;
using Learly.Application.Services.Common;

namespace Learly.Application.Services.Alunos;

public interface IAlunosService
{
    Task<AlunosListagemResultado> ListarAsync(
        ListarAlunosQuery query,
        AppUserContext uc,
        CancellationToken cancellationToken = default);

    Task<AlunoDetalheResultado> ObterPorIdAsync(
        int id,
        AppUserContext uc,
        CancellationToken cancellationToken = default);

    Task<CriarAlunoResultado> CriarAlunoAsync(
        CriarAlunoRequest request,
        AppUserContext uc,
        CancellationToken cancellationToken = default);
}
