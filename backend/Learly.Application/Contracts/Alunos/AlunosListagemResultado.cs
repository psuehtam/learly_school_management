using Learly.Application.Contracts.Alunos.Responses;

namespace Learly.Application.Contracts.Alunos;

public enum AlunosListagemFalha
{
    Nenhuma,
    AcessoNegado,
    Validacao
}

public sealed record AlunosListagemResultado(
    bool Ok,
    IReadOnlyList<AlunoListItemResponse> Itens,
    string? Mensagem,
    AlunosListagemFalha Falha);
