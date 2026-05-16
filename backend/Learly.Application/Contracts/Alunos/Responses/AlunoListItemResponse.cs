namespace Learly.Application.Contracts.Alunos.Responses;

public sealed class AlunoListItemResponse
{
    public int Id { get; init; }
    public int EscolaId { get; init; }
    public string Nome { get; init; } = "";
    public string Sobrenome { get; init; } = "";
    public string? Cpf { get; init; }
    public string Status { get; init; } = "";
}
