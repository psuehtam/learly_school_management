using Learly.Application.Contracts.Livros.Responses;

namespace Learly.Application.Contracts.Livros;

public sealed record LivrosEscolaListagemResultado(bool Ok, IReadOnlyList<LivroEscolaResponse> Itens, string? Mensagem, LivrosEscolaFalha Falha);

public sealed record LivrosEscolaDetalheResultado(bool Ok, LivroEscolaResponse? Livro, string? Mensagem, LivrosEscolaFalha Falha);

public sealed record LivrosEscolaCriacaoResultado(bool Ok, LivroEscolaResponse? Livro, string? Mensagem, LivrosEscolaFalha Falha);

public sealed record LivrosEscolaAtualizacaoResultado(bool Ok, LivroEscolaResponse? Livro, string? Mensagem, LivrosEscolaFalha Falha);
