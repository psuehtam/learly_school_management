namespace Learly.Application.Contracts.Contratos.Requests;

public sealed class EditarContratoTemplateRequest
{
    public string Nome { get; init; } = string.Empty;
    public string ConteudoHtml { get; init; } = string.Empty;
}
