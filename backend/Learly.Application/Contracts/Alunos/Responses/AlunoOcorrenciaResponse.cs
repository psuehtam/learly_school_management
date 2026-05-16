namespace Learly.Application.Contracts.Alunos.Responses;

public sealed class AlunoOcorrenciaResponse
{
    public int Id { get; init; }
    public string Data { get; init; } = "";
    public string Hora { get; init; } = "";
    public string Tipo { get; init; } = "";
    public string Descricao { get; init; } = "";
    public string? Resolucao { get; init; }
    public string AulaVinculada { get; init; } = "";
    public string Autor { get; init; } = "";
}
