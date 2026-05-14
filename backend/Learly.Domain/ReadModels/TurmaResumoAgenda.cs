namespace Learly.Domain.ReadModels;

/// <summary>Dados mínimos de turma + livro para exibir na agenda.</summary>
public sealed record TurmaResumoAgenda(string TurmaNome, string LivroNome);
