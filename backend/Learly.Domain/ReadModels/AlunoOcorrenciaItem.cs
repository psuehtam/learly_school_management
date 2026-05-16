namespace Learly.Domain.ReadModels;

public sealed record AlunoOcorrenciaItem(
    int Id,
    string Tipo,
    DateOnly DataOcorrencia,
    TimeOnly HoraOcorrencia,
    string Descricao,
    string? Resolucao,
    string AutorNome,
    string? AulaVinculada);
