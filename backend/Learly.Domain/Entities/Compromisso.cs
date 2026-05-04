using Learly.Domain.Exceptions;

namespace Learly.Domain.Entities;

public sealed class Compromisso
{
    public static class Statuses
    {
        public const string Pendente = "Pendente";
        public const string EmAndamento = "Em andamento";
        public const string Concluido = "Concluido";
        public const string Cancelado = "Cancelado";
    }

    public int Id { get; internal set; }
    public int EscolaId { get; internal set; }
    public int UsuarioId { get; internal set; }

    private string _titulo = string.Empty;
    public string Titulo
    {
        get => _titulo;
        internal set => _titulo = string.IsNullOrWhiteSpace(value) ? throw new DomainException("Titulo e obrigatorio.") : value.Trim();
    }

    public string? Descricao { get; internal set; }
    public DateTime DataInicio { get; internal set; }
    public DateTime DataFim { get; internal set; }
    public string? Local { get; internal set; }
    public string Tipo { get; internal set; } = "Outro";
    public string Prioridade { get; internal set; } = "Media";
    public string Status { get; internal set; } = Statuses.Pendente;
    public int? LembreteMinutos { get; internal set; }
    public string? Cor { get; internal set; }

    public void ValidarIntervalo()
    {
        if (DataFim <= DataInicio)
            throw new DomainException("Data fim deve ser maior que data inicio.");

        if (DateOnly.FromDateTime(DataInicio) != DateOnly.FromDateTime(DataFim))
            throw new DomainException("Compromisso deve iniciar e terminar no mesmo dia.");
    }
}
