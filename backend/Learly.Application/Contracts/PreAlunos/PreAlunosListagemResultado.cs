using Learly.Application.Contracts.PreAlunos.Responses;

namespace Learly.Application.Contracts.PreAlunos;

public enum PreAlunosListagemFalha
{
    Nenhuma,
    AcessoNegado,
    Validacao
}

public sealed record PreAlunosListagemResultado(
    bool Ok,
    IReadOnlyList<PreAlunoListItemResponse> Itens,
    string? Mensagem,
    PreAlunosListagemFalha Falha);
