using System.ComponentModel.DataAnnotations;

namespace Learly.Application.Contracts.Livros.Requests;

public sealed class AtualizarLivroCapituloAulasItemRequest
{
    [Range(1, int.MaxValue)]
    public int CapituloId { get; set; }

    [Range(1, 500)]
    public int QtdAulasPrevistas { get; set; }
}
