namespace Learly.Application.Contracts.Livros.Requests;

/// <summary>Alteração parcial — envie apenas os campos que deseja mudar.</summary>
public sealed class AtualizarLivroEscolaRequest
{
    public string? Nome { get; set; }

    /// <summary><c>Ativo</c> ou <c>Inativo</c>.</summary>
    public string? Status { get; set; }

    /// <summary>
    /// Atualiza <c>qtd_aulas_previstas</c> por capítulo. Deve conter exatamente um item por capítulo do livro
    /// (sem duplicar <see cref="AtualizarLivroCapituloAulasItemRequest.CapituloId"/>).
    /// </summary>
    public IReadOnlyList<AtualizarLivroCapituloAulasItemRequest>? CapitulosAulas { get; set; }

    /// <summary>Novos capítulos no final do livro (respeita limite total de capítulos por livro).</summary>
    public IReadOnlyList<NovoCapituloLivroEscolaItemRequest>? CapitulosNovos { get; set; }
}
