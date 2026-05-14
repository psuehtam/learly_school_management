using System.ComponentModel.DataAnnotations;

namespace Learly.Application.Contracts.Compromissos.Requests;

public sealed class CriarCompromissoRequest
{
    [Required]
    [MaxLength(255)]
    public string Titulo { get; init; } = string.Empty;

    public string? Descricao { get; init; }

    [Required]
    public DateTime DataInicio { get; init; }

    [Required]
    public DateTime DataFim { get; init; }

    [MaxLength(255)]
    public string? Local { get; init; }

    public string? Tipo { get; init; }
    public string? Prioridade { get; init; }

    [Range(0, 10080)]
    public int? LembreteMinutos { get; init; }

    [RegularExpression("^#[0-9A-Fa-f]{6}$")]
    public string? Cor { get; init; }

    [Required]
    [MinLength(1)]
    public IReadOnlyList<int> ParticipantesUsuarioIds { get; init; } = [];
}
