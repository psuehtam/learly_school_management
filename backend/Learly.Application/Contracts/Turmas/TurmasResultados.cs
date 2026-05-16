using Learly.Application.Contracts.Turmas.Responses;

namespace Learly.Application.Contracts.Turmas;

public sealed record TurmasListagemResultado(bool Ok, IReadOnlyList<TurmaResponse> Itens, string? Mensagem, TurmasFalha Falha);

public sealed record TurmaDetalheResultado(bool Ok, TurmaResponse? Turma, string? Mensagem, TurmasFalha Falha);

public sealed record TurmaOperacaoResultado(bool Ok, TurmaResponse? Turma, string? Mensagem, TurmasFalha Falha, int StatusCode = 400);
