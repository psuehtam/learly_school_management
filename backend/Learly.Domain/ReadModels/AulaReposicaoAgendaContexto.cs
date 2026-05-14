namespace Learly.Domain.ReadModels;

/// <summary>Contexto de reposição (aluno + aula original) via vínculo em presencas.</summary>
public sealed record AulaReposicaoAgendaContexto(
    string AlunoNomeCompleto,
    int NumeroAulaOriginal,
    DateOnly DataAulaOriginal);
