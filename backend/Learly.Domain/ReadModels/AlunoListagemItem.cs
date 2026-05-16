namespace Learly.Domain.ReadModels;

public sealed record AlunoListagemItem(
    int Id,
    int EscolaId,
    string Nome,
    string Sobrenome,
    string? Cpf,
    string Status);
