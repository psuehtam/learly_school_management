namespace Learly.Application.Contracts.Alunos.Requests;

public sealed class ListarAlunosQuery
{
    public string? Status { get; set; }
    public string? Busca { get; set; }
    public int Limite { get; set; } = 80;
}
