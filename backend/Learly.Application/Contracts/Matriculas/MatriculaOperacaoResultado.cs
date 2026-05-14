namespace Learly.Application.Contracts.Matriculas;

public sealed record MatriculaOperacaoResultado(bool Ok, string? Mensagem, int StatusCode);
