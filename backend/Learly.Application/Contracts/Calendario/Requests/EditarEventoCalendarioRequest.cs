namespace Learly.Application.Contracts.Calendario.Requests;

public sealed class EditarEventoCalendarioRequest
{
    public DateOnly? DataEvento { get; init; }
    public string? TipoEvento { get; init; }
    public string? Descricao { get; init; }
}
