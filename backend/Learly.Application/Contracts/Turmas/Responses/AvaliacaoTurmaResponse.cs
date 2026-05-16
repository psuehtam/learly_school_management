namespace Learly.Application.Contracts.Turmas.Responses;

public sealed class AvaliacaoTurmaResponse
{
    public int Id { get; init; }
    public int TurmaId { get; init; }
    public int AlunoId { get; init; }
    public string Tipo { get; init; } = string.Empty;
    public decimal Nota { get; init; }
}
