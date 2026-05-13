using Learly.Application.Contracts.HorariosFuncionamento;
using Learly.Application.Contracts.HorariosFuncionamento.Requests;
using Learly.Application.Services.Common;

namespace Learly.Application.Services.HorariosFuncionamento;

public interface IHorariosFuncionamentoService
{
    Task<HorariosFuncionamentoListagemResultado> ListarAsync(
        AppUserContext uc,
        CancellationToken cancellationToken = default);

    Task<HorariosFuncionamentoAtualizacaoResultado> AtualizarAsync(
        AtualizarHorariosEscolaRequest request,
        AppUserContext uc,
        CancellationToken cancellationToken = default);
}
