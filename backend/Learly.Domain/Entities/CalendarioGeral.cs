using Learly.Domain.Exceptions;

namespace Learly.Domain.Entities;

public sealed class CalendarioGeral
{
    public static class TiposEvento
    {
        public const string Aula = "AULA";
        public const string SemAula = "SEM AULA";
        public const string Feriado = "FERIADO";
        public const string Recesso = "RECESSO";
    }

    private static readonly HashSet<string> TiposValidos = new(StringComparer.OrdinalIgnoreCase)
    {
        TiposEvento.Aula,
        TiposEvento.SemAula,
        TiposEvento.Feriado,
        TiposEvento.Recesso
    };

    public int Id { get; internal set; }
    public int EscolaId { get; internal set; }
    public DateOnly DataEvento { get; internal set; }

    private string _tipoEvento = TiposEvento.Aula;
    public string TipoEvento
    {
        get => _tipoEvento;
        internal set => _tipoEvento = NormalizarTipoEvento(value);
    }

    private string? _descricao;
    public string? Descricao
    {
        get => _descricao;
        internal set => _descricao = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public bool SuspendeAula { get; internal set; }
    public int UsuarioId { get; internal set; }
    public DateTime DataCriacao { get; internal set; }

    public static bool TipoSuspendeAula(string tipoEvento)
    {
        var tipo = NormalizarTipoEvento(tipoEvento);
        return !string.Equals(tipo, TiposEvento.Aula, StringComparison.OrdinalIgnoreCase);
    }

    public static string NormalizarTipoEvento(string? tipoEvento)
    {
        if (string.IsNullOrWhiteSpace(tipoEvento))
            throw new DomainException("Tipo de evento e obrigatorio.");

        var tipo = tipoEvento.Trim();
        if (!TiposValidos.Contains(tipo))
            throw new DomainException("Tipo de evento invalido.");

        if (string.Equals(tipo, TiposEvento.SemAula, StringComparison.OrdinalIgnoreCase)) return TiposEvento.SemAula;
        if (string.Equals(tipo, TiposEvento.Feriado, StringComparison.OrdinalIgnoreCase)) return TiposEvento.Feriado;
        if (string.Equals(tipo, TiposEvento.Recesso, StringComparison.OrdinalIgnoreCase)) return TiposEvento.Recesso;
        return TiposEvento.Aula;
    }
}
