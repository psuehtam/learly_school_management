namespace Learly.Application.Contracts.PreAlunos;

public enum PreAlunoCriacaoFalha
{
    Nenhuma,
    AcessoNegado,
    Validacao,
    Conflito
}

public sealed record PreAlunoCriacaoResultado(
    bool Ok,
    int? Id,
    string? Mensagem,
    PreAlunoCriacaoFalha Falha);
