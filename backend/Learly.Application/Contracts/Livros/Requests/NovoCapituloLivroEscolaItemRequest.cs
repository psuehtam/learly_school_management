using System.ComponentModel.DataAnnotations;

namespace Learly.Application.Contracts.Livros.Requests;

/// <summary>Novo capítulo ao final de um livro já existente.</summary>
public sealed class NovoCapituloLivroEscolaItemRequest
{
    /// <summary>Se vazio, o sistema gera nome no padrão <c>Capítulo N</c>.</summary>
    [MaxLength(100)]
    public string? Nome { get; set; }

    [Range(1, 500)]
    public int QtdAulasPrevistas { get; set; }
}
