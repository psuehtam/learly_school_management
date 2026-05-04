namespace Learly.Application.Contracts.Matriculas.Responses;

public sealed class MatriculaListItemResponse
{
    public int Id { get; init; }
    public int EscolaId { get; init; }
    public int AlunoId { get; init; }
    public int? TurmaId { get; init; }
    public DateOnly DataMatricula { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime DataCriacao { get; init; }
    public DateTime DataAtualizacao { get; init; }
}
