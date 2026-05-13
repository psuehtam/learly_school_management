namespace Learly.Application.Contracts.Matriculas;

public enum MatriculaCriacaoFalha
{
    Nenhuma,
    AcessoNegado,
    Validacao,
    Conflito
}

public sealed record MatriculaCriacaoResultado(bool Ok, int? Id, string? Mensagem, MatriculaCriacaoFalha Falha);
