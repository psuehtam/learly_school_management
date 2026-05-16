namespace Learly.Domain.ReadModels;

public sealed record TurmaListagemItem(
    int Id,
    int ProfessorId,
    string ProfessorNome,
    int LivroId,
    string LivroNome,
    string Nome,
    string? Sala,
    TimeOnly? Horario,
    TimeOnly? HorarioFim,
    DateOnly? DataInicio,
    DateOnly? DataTerminoPrevista,
    string Status,
    string? Observacoes,
    IReadOnlyList<int> DiasSemana,
    int TotalAlunosAtivos);
