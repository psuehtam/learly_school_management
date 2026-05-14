namespace Learly.Application.Contracts.Alunos.Responses;

public enum CriarAlunoFalha
{
    Nenhuma,
    AcessoNegado,
    Validacao,
    Conflito
}

public sealed record CriarAlunoResultado(bool Ok, int? AlunoId, int? MatriculaId, string? Mensagem, CriarAlunoFalha Falha);
