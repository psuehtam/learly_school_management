namespace Learly.Application.Contracts.Contratos.Responses;

public sealed record ContratoGeradoResponse(
    int Id,
    int PreAlunoId,
    int TemplateId,
    string NomeTemplate,
    string ConteudoGeradoHtml,
    DateTime DataGeracao);
