using System.Text.Json.Serialization;

namespace Learly.Application.Contracts.Matriculas.Responses;

public sealed class MatriculaListItemResponse
{
    public int Id { get; init; }
    public int EscolaId { get; init; }
    public int AlunoId { get; init; }

    [JsonPropertyName("alunoNomeCompleto")]
    public string AlunoNomeCompleto { get; init; } = string.Empty;

    public int? TurmaId { get; init; }

    [JsonPropertyName("turmaNome")]
    public string? TurmaNome { get; init; }
    public DateOnly DataMatricula { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime DataCriacao { get; init; }
    public DateTime DataAtualizacao { get; init; }
}
