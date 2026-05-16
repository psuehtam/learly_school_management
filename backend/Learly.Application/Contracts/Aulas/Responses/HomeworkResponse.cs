namespace Learly.Application.Contracts.Aulas.Responses;

public sealed class HomeworkResponse
{
    public int Id { get; init; }
    public int AulaId { get; init; }
    public int AlunoId { get; init; }
    public decimal? Nota { get; init; }
}
