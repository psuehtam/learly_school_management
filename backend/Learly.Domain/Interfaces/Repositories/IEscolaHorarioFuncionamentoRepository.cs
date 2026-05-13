using Learly.Domain.Entities;

namespace Learly.Domain.Interfaces.Repositories;

public interface IEscolaHorarioFuncionamentoRepository
{
    Task<bool> PossuiConfiguracaoAsync(int escolaId, CancellationToken cancellationToken = default);

    Task<EscolaHorarioFuncionamento?> ObterPorDiaSemanaAsync(
        int escolaId,
        int diaSemana,
        CancellationToken cancellationToken = default);

    Task<List<EscolaHorarioFuncionamento>> ListarRastreadosPorEscolaAsync(
        int escolaId,
        CancellationToken cancellationToken = default);

    Task<List<EscolaHorarioFuncionamento>> ListarPorEscolaAsync(
        int escolaId,
        CancellationToken cancellationToken = default);

    Task AdicionarAsync(EscolaHorarioFuncionamento horario, CancellationToken cancellationToken = default);
}
