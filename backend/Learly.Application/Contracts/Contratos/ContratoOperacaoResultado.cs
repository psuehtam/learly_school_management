namespace Learly.Application.Contracts.Contratos;

public sealed record ContratoTemplateOperacaoResultado(bool Ok, string? Mensagem, int StatusCode);

public sealed record ContratoGeradoOperacaoResultado(
    bool Ok,
    string? Mensagem,
    int StatusCode,
    ContratoGeradoData? Data = null);

public sealed record ContratoGeradoData(
    int Id,
    int TemplateId,
    string NomeTemplate,
    string ConteudoGeradoHtml,
    DateTime DataGeracao);
