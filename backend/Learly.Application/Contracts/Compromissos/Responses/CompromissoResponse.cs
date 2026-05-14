namespace Learly.Application.Contracts.Compromissos.Responses;

public sealed class CompromissoResponse
{
    public int Id { get; init; }
    public string Titulo { get; init; } = string.Empty;
    public string? Descricao { get; init; }
    public DateTime DataInicio { get; init; }
    public DateTime DataFim { get; init; }
    public string? Local { get; init; }
    public string Tipo { get; init; } = "Outro";
    public string Prioridade { get; init; } = "Media";
    public string Status { get; init; } = "Pendente";
    public int? LembreteMinutos { get; init; }
    public string? Cor { get; init; }
    public IReadOnlyList<int> ParticipantesUsuarioIds { get; init; } = [];
}
