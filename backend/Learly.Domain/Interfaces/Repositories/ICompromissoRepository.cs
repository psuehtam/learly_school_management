using Learly.Domain.Entities;

namespace Learly.Domain.Interfaces.Repositories;

public interface ICompromissoRepository : IRepository<Compromisso, int>
{
    Task<IReadOnlyList<Compromisso>> ListarPorEscolaEUsuarioAsync(
        int escolaId,
        int usuarioId,
        CancellationToken cancellationToken = default);

    Task<Compromisso?> ObterPorIdEEscolaAsync(
        int compromissoId,
        int escolaId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Compromisso>> ListarAgendaGlobalAsync(
        int escolaId,
        DateOnly data,
        int? usuarioId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<int>> ListarParticipantesIdsAsync(
        int compromissoId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<int, IReadOnlyList<int>>> ListarParticipantesIdsPorCompromissosAsync(
        IReadOnlyCollection<int> compromissoIds,
        CancellationToken cancellationToken = default);

    Task DefinirParticipantesAsync(
        int compromissoId,
        IReadOnlyCollection<int> usuarioIds,
        CancellationToken cancellationToken = default);

    Task<bool> ExisteConflitoCompromissoAsync(
        int escolaId,
        IReadOnlyCollection<int> usuarioIds,
        DateTime dataInicio,
        DateTime dataFim,
        int? compromissoIgnoradoId,
        CancellationToken cancellationToken = default);

    Task<bool> ExisteConflitoAulaProfessorAsync(
        int escolaId,
        IReadOnlyCollection<int> usuarioIds,
        DateTime dataInicio,
        DateTime dataFim,
        CancellationToken cancellationToken = default);

    Task<bool> ExisteCompromissoAtivoNoDiaAsync(
        int escolaId,
        DateOnly data,
        CancellationToken cancellationToken = default);
}
