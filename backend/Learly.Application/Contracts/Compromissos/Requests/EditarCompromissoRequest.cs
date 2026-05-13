using System.ComponentModel.DataAnnotations;

namespace Learly.Application.Contracts.Compromissos.Requests;

public sealed class EditarCompromissoRequest
{
    [MaxLength(255)]
    public string? Titulo { get; init; }

    public string? Descricao { get; init; }
    public DateTime? DataInicio { get; init; }
    public DateTime? DataFim { get; init; }

    [MaxLength(255)]
    public string? Local { get; init; }

    public string? Tipo { get; init; }
    public string? Prioridade { get; init; }
    public string? Status { get; init; }

    [Range(0, 10080)]
    public int? LembreteMinutos { get; init; }

    [RegularExpression("^#[0-9A-Fa-f]{6}$")]
    public string? Cor { get; init; }

    [MinLength(1)]
    public IReadOnlyList<int>? ParticipantesUsuarioIds { get; init; }
}
