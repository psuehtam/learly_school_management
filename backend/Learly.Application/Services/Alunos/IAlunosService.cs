using Learly.Application.Contracts.Alunos.Requests;
using Learly.Application.Contracts.Alunos.Responses;
using Learly.Application.Services.Common;

namespace Learly.Application.Services.Alunos;

public interface IAlunosService
{
    Task<CriarAlunoResultado> CriarAlunoAsync(
        CriarAlunoRequest request,
        AppUserContext uc,
        CancellationToken cancellationToken = default);
}
