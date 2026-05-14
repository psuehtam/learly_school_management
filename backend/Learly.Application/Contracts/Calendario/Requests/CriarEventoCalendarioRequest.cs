namespace Learly.Application.Contracts.Calendario.Requests;

public sealed class CriarEventoCalendarioRequest
{
    public DateOnly DataEvento { get; init; }
    public string TipoEvento { get; init; } = string.Empty;
    public string? Descricao { get; init; }
}
