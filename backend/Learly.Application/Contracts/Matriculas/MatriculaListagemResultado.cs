using Learly.Application.Contracts.Matriculas.Responses;

namespace Learly.Application.Contracts.Matriculas;

public enum MatriculaListagemFalha
{
    Nenhuma,
    AcessoNegado,
    Validacao
}

public sealed record MatriculaListagemResultado(
    bool Ok,
    IReadOnlyList<MatriculaListItemResponse> Itens,
    string? Mensagem,
    MatriculaListagemFalha Falha);
