namespace Learly.Application.Contracts.Alunos.Responses;

public sealed class AlunoDocumentoResponse
{
    public int Id { get; init; }
    public string Nome { get; init; } = "";
    public string Tipo { get; init; } = "";
    public string DataEnvio { get; init; } = "";
    public string Tamanho { get; init; } = "";
    public string Autor { get; init; } = "";
}
