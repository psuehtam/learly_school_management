namespace Learly.Application.Contracts.Alunos.Responses;

public sealed class AlunoFaltaResponse
{
    public int Id { get; init; }
    public string Data { get; init; } = "";
    public string Book { get; init; } = "";
    public string Aula { get; init; } = "";
    public bool Justificada { get; init; }
    public string? Motivo { get; init; }
    public string? AutorJustificativa { get; init; }
    public string? DataJustificativa { get; init; }
}
