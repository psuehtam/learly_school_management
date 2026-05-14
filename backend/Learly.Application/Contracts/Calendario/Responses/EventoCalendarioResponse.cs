namespace Learly.Application.Contracts.Calendario.Responses;

public sealed class EventoCalendarioResponse
{
    public int Id { get; init; }
    public DateOnly DataEvento { get; init; }
    public string TipoEvento { get; init; } = string.Empty;
    public string? Descricao { get; init; }
    public bool SuspendeAula { get; init; }
}
