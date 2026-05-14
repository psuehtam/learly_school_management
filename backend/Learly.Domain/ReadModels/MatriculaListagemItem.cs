namespace Learly.Domain.ReadModels;

/// <summary>
/// Linha de listagem de matrículas com nome do aluno e da turma (somente leitura / consulta).
/// </summary>
public sealed record MatriculaListagemItem(
    int Id,
    int EscolaId,
    int AlunoId,
    string AlunoNomeCompleto,
    int? TurmaId,
    string? TurmaNome,
    DateOnly DataMatricula,
    string Status,
    DateTime DataCriacao,
    DateTime DataAtualizacao);
