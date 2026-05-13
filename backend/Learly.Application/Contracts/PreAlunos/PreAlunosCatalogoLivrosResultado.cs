using Learly.Application.Contracts.PreAlunos.Responses;

namespace Learly.Application.Contracts.PreAlunos;

public enum PreAlunosCatalogoLivrosFalha
{
    Nenhuma,
    AcessoNegado
}

public sealed record PreAlunosCatalogoLivrosResultado(
    bool Ok,
    IReadOnlyList<LivroInteresseOpcaoResponse> Itens,
    string? Mensagem,
    PreAlunosCatalogoLivrosFalha Falha);
