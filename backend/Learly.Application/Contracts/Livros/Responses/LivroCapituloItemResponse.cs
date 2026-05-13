namespace Learly.Application.Contracts.Livros.Responses;

public sealed record LivroCapituloItemResponse(
    int Id,
    string Nome,
    int QtdAulasPrevistas,
    string Status);
