namespace Learly.Application.Contracts.Alunos.Requests;

public sealed class SalvarAlunoOcorrenciaRequest
{
    public string Data { get; init; } = "";
    public string Hora { get; init; } = "";
    public string Tipo { get; init; } = "";
    public string Descricao { get; init; } = "";
    public string? Resolucao { get; init; }
    public string? AulaVinculada { get; init; }
}
