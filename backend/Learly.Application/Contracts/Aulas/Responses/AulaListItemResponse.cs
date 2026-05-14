namespace Learly.Application.Contracts.Aulas.Responses;

public sealed class AulaListItemResponse
{
    public int Id { get; init; }
    public int TurmaId { get; init; }
    public int? CapituloId { get; init; }
    public int ProfessorId { get; init; }
    public int NumeroAula { get; init; }
    public DateOnly DataAula { get; init; }
    public TimeOnly HorarioInicio { get; init; }
    public TimeOnly HorarioFim { get; init; }
    public string? ConteudoDado { get; init; }
    public string TipoAula { get; init; } = "Normal";
    public string Status { get; init; } = "Agendada";

    /// <summary>Preenchido na listagem para exibir na agenda.</summary>
    public string? TurmaNome { get; set; }

    public string? LivroNome { get; set; }

    public string? ReposicaoAlunoNome { get; set; }

    public int? ReposicaoAulaOriginalNumero { get; set; }

    public DateOnly? ReposicaoAulaOriginalData { get; set; }
}
