using System.ComponentModel.DataAnnotations;

namespace Learly.Application.Contracts.Livros.Requests;

public sealed class CriarLivroEscolaRequest
{
    /// <summary>Nome do livro/nível (até 150 caracteres).</summary>
    [Required]
    [MaxLength(150)]
    public required string Nome { get; set; }

    /// <summary>Quantidade de registros em <c>capitulos</c> para este livro.</summary>
    [Range(1, 200)]
    public int QuantidadeCapitulos { get; set; }

    /// <summary>
    /// Um inteiro por capítulo, na ordem (Capítulo 1 … N): valor de <c>qtd_aulas_previstas</c> em cada linha de <c>capitulos</c>.
    /// Deve ter exatamente <see cref="QuantidadeCapitulos"/> elementos.
    /// </summary>
    [Required]
    [MinLength(1)]
    [MaxLength(200)]
    public required IList<int> AulasPrevistasPorCapitulo { get; set; }
}
