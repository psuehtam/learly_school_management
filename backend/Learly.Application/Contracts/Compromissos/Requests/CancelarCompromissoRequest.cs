namespace Learly.Application.Contracts.Compromissos.Requests;

public sealed class CancelarCompromissoRequest
{
    public string Motivo { get; init; } = string.Empty;
}
