namespace Learly.Application.Contracts.PreAlunos;

public sealed record PreAlunoOperacaoResultado(bool Ok, string? Mensagem, int StatusCode);
