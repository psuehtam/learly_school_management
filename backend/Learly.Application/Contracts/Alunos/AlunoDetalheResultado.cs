using Learly.Application.Contracts.Alunos.Responses;

namespace Learly.Application.Contracts.Alunos;

public enum AlunoDetalheFalha
{
    Nenhuma,
    AcessoNegado,
    NaoEncontrado
}

public sealed record AlunoDetalheResultado(
    bool Ok,
    AlunoDetalheResponse? Aluno,
    string? Mensagem,
    AlunoDetalheFalha Falha);
