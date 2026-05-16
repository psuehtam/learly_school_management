namespace Learly.Application.Contracts.Turmas.Responses;

public sealed class TurmaResponse
{
    public int Id { get; init; }
    public int EscolaId { get; init; }
    public int ProfessorId { get; init; }
    public string? ProfessorNome { get; init; }
    public int LivroId { get; init; }
    public string? LivroNome { get; init; }
    public string Nome { get; init; } = "";
    public string? Sala { get; init; }
    public string? HorarioInicio { get; init; }
    public string? HorarioFim { get; init; }
    public string? DataInicio { get; init; }
    public string? DataTerminoPrevista { get; init; }
    public string Status { get; init; } = "";
    public string? Observacoes { get; init; }
    public IReadOnlyList<int> DiasSemana { get; init; } = [];
    public int TotalAlunosAtivos { get; init; }
    public int TotalAulasPrevistasLivro { get; init; }
}
