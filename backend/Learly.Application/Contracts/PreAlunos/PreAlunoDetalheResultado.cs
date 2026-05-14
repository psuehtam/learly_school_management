using Learly.Application.Contracts.PreAlunos.Responses;

namespace Learly.Application.Contracts.PreAlunos;

public enum PreAlunoDetalheFalha
{
    Nenhuma,
    AcessoNegado,
    NaoEncontrado
}

public sealed record PreAlunoDetalheResultado(
    bool Ok,
    PreAlunoDetalheResponse? Detalhe,
    string? Mensagem,
    PreAlunoDetalheFalha Falha);
