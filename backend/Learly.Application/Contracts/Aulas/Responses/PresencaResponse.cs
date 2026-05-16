namespace Learly.Application.Contracts.Aulas.Responses;

public sealed class PresencaResponse
{
    public int Id { get; init; }
    public int AulaId { get; init; }
    public int AlunoId { get; init; }
    public string StatusPresenca { get; init; } = string.Empty;
}
