namespace Learly.Application.Contracts.Contratos.Responses;

public sealed record ContratoTemplateListItemResponse(
    int Id,
    string Nome,
    int Versao,
    bool Ativo,
    DateTime DataCriacao,
    string ConteudoHtml);
