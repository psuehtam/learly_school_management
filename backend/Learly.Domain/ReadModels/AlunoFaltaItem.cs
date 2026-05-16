namespace Learly.Domain.ReadModels;

public sealed record AlunoFaltaItem(
    int PresencaId,
    string StatusPresenca,
    DateOnly DataAula,
    string TurmaNome,
    string AulaTitulo);
