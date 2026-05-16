using Learly.Domain.Entities;
using Learly.Domain.ReadModels;

namespace Learly.Domain.Interfaces.Repositories;

public interface ITurmaRepository : IRepository<Turma, int>
{
    Task<IReadOnlyList<TurmaListagemItem>> ListarDetalhadoPorEscolaAsync(
        int escolaId,
        string? status,
        int? professorId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Turma>> ListarPorEscolaAsync(int escolaId, CancellationToken cancellationToken = default);

    Task<Turma?> ObterPorIdEEscolaAsync(int turmaId, int escolaId, CancellationToken cancellationToken = default);

    Task<Turma?> ObterRastreadaPorIdEEscolaAsync(int turmaId, int escolaId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<int>> ListarDiasSemanaAsync(int turmaId, CancellationToken cancellationToken = default);

    Task SubstituirDiasSemanaAsync(int escolaId, int turmaId, IReadOnlyList<int> diasSemana, CancellationToken cancellationToken = default);

    Task<int> ContarMatriculasAtivasAsync(int turmaId, CancellationToken cancellationToken = default);

    Task<bool> PossuiAulasRealizadasAsync(int turmaId, CancellationToken cancellationToken = default);

    Task AtualizarProfessorAulasAgendadasAsync(int turmaId, int professorId, CancellationToken cancellationToken = default);

    Task<int> ContarTurmasPorLivroAsync(int escolaId, int livroId, CancellationToken cancellationToken = default);

    Task<bool> ExisteConflitoHorarioProfessorAsync(
        int escolaId,
        int professorId,
        int diaSemana,
        TimeOnly horarioInicio,
        TimeOnly horarioFim,
        int? ignorarTurmaId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<int>> ListarIdsTurmasEmAndamentoAsync(int escolaId, CancellationToken cancellationToken = default);

    Task RemoverAulasAgendadasDaTurmaAsync(int turmaId, CancellationToken cancellationToken = default);

    Task RemoverProgressoCapitulosAsync(int turmaId, CancellationToken cancellationToken = default);

    Task InicializarProgressoCapitulosAsync(
        int escolaId,
        int turmaId,
        IReadOnlyList<int> capituloIds,
        CancellationToken cancellationToken = default);

    Task<bool> TodosCapitulosConcluidosAsync(int turmaId, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<int, TurmaResumoAgenda>> ObterResumoParaAgendaAsync(
        int escolaId,
        IReadOnlyList<int> turmaIds,
        CancellationToken cancellationToken = default);
}
