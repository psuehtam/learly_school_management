using Learly.Domain.ReadModels;

namespace Learly.Domain.Interfaces.Repositories;

public interface IAlunoPerfilRepository
{
    Task<IReadOnlyList<AlunoOcorrenciaItem>> ListarOcorrenciasAsync(
        int escolaId,
        int alunoId,
        CancellationToken cancellationToken = default);

    Task<int> CriarOcorrenciaAsync(
        int escolaId,
        int alunoId,
        int usuarioId,
        string tipo,
        DateOnly dataOcorrencia,
        TimeOnly horaOcorrencia,
        string descricao,
        string? resolucao,
        int? aulaId,
        CancellationToken cancellationToken = default);

    Task<bool> AtualizarOcorrenciaAsync(
        int escolaId,
        int alunoId,
        int ocorrenciaId,
        string tipo,
        DateOnly dataOcorrencia,
        TimeOnly horaOcorrencia,
        string descricao,
        string? resolucao,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AlunoFaltaItem>> ListarFaltasAsync(
        int escolaId,
        int alunoId,
        CancellationToken cancellationToken = default);

    Task<bool> JustificarFaltaAsync(
        int escolaId,
        int alunoId,
        int presencaId,
        CancellationToken cancellationToken = default);
}
