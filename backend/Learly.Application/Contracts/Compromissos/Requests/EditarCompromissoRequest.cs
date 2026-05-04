namespace Learly.Application.Contracts.Compromissos.Requests;

public sealed class EditarCompromissoRequest
{
    public string? Titulo { get; init; }
    public string? Descricao { get; init; }
    public DateTime? DataInicio { get; init; }
    public DateTime? DataFim { get; init; }
    public string? Local { get; init; }
    public string? Tipo { get; init; }
    public string? Prioridade { get; init; }
    public string? Status { get; init; }
    public int? LembreteMinutos { get; init; }
    public string? Cor { get; init; }
    public IReadOnlyList<int>? ParticipantesUsuarioIds { get; init; }
}
