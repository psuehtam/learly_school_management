using Learly.Domain.Exceptions;
using System.Text.RegularExpressions;

namespace Learly.Domain.Entities;

public sealed class Compromisso
{
    private static readonly Regex CorHexRegex = new("^#[0-9A-Fa-f]{6}$", RegexOptions.Compiled);

    public static class Tipos
    {
        public const string Reuniao = "Reuniao";
        public const string Evento = "Evento";
        public const string Tarefa = "Tarefa";
        public const string Outro = "Outro";
    }

    public static class Prioridades
    {
        public const string Alta = "Alta";
        public const string Media = "Media";
        public const string Baixa = "Baixa";
    }

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
        internal set => _titulo = ValidarTitulo(value);
    }

    private string? _descricao;
    public string? Descricao
    {
        get => _descricao;
        internal set => _descricao = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public DateTime DataInicio { get; internal set; }
    public DateTime DataFim { get; internal set; }

    private string? _local;
    public string? Local
    {
        get => _local;
        internal set => _local = ValidarLocal(value);
    }

    private string _tipo = Tipos.Outro;
    public string Tipo
    {
        get => _tipo;
        internal set => _tipo = NormalizarTipo(value);
    }

    private string _prioridade = Prioridades.Media;
    public string Prioridade
    {
        get => _prioridade;
        internal set => _prioridade = NormalizarPrioridade(value);
    }

    private string _status = Statuses.Pendente;
    public string Status
    {
        get => _status;
        internal set => _status = NormalizarStatus(value);
    }

    private int? _lembreteMinutos;
    public int? LembreteMinutos
    {
        get => _lembreteMinutos;
        internal set => _lembreteMinutos = ValidarLembreteMinutos(value);
    }

    private string? _cor;
    public string? Cor
    {
        get => _cor;
        internal set => _cor = ValidarCor(value);
    }

    public void ValidarIntervalo()
    {
        if (DataFim <= DataInicio)
            throw new DomainException("Data fim deve ser maior que data inicio.");

        if (DateOnly.FromDateTime(DataInicio) != DateOnly.FromDateTime(DataFim))
            throw new DomainException("Compromisso deve iniciar e terminar no mesmo dia.");
    }

    public bool PodeTransicionarPara(string proximoStatus)
    {
        var atual = Status;
        var proximo = NormalizarStatus(proximoStatus);
        if (string.Equals(atual, proximo, StringComparison.Ordinal))
            return true;

        return atual switch
        {
            Statuses.Pendente =>
                string.Equals(proximo, Statuses.EmAndamento, StringComparison.Ordinal)
                || string.Equals(proximo, Statuses.Concluido, StringComparison.Ordinal)
                || string.Equals(proximo, Statuses.Cancelado, StringComparison.Ordinal),
            Statuses.EmAndamento =>
                string.Equals(proximo, Statuses.Concluido, StringComparison.Ordinal)
                || string.Equals(proximo, Statuses.Cancelado, StringComparison.Ordinal),
            Statuses.Concluido => false,
            Statuses.Cancelado => false,
            _ => false
        };
    }

    public void AlterarStatus(string novoStatus)
    {
        var normalized = NormalizarStatus(novoStatus);
        if (!PodeTransicionarPara(normalized))
            throw new DomainException($"Transicao de status invalida: {Status} -> {normalized}.");

        Status = normalized;
    }

    private static string ValidarTitulo(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Titulo e obrigatorio.");

        var trimmed = value.Trim();
        if (trimmed.Length > 255)
            throw new DomainException("Titulo deve ter ate 255 caracteres.");

        return trimmed;
    }

    private static string? ValidarLocal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        if (trimmed.Length > 255)
            throw new DomainException("Local deve ter ate 255 caracteres.");

        return trimmed;
    }

    private static string NormalizarTipo(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Tipos.Outro;

        var trimmed = value.Trim();
        if (string.Equals(trimmed, Tipos.Reuniao, StringComparison.OrdinalIgnoreCase)) return Tipos.Reuniao;
        if (string.Equals(trimmed, Tipos.Evento, StringComparison.OrdinalIgnoreCase)) return Tipos.Evento;
        if (string.Equals(trimmed, Tipos.Tarefa, StringComparison.OrdinalIgnoreCase)) return Tipos.Tarefa;
        if (string.Equals(trimmed, Tipos.Outro, StringComparison.OrdinalIgnoreCase)) return Tipos.Outro;

        throw new DomainException("Tipo invalido. Use Reuniao, Evento, Tarefa ou Outro.");
    }

    private static string NormalizarPrioridade(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Prioridades.Media;

        var trimmed = value.Trim();
        if (string.Equals(trimmed, Prioridades.Alta, StringComparison.OrdinalIgnoreCase)) return Prioridades.Alta;
        if (string.Equals(trimmed, Prioridades.Media, StringComparison.OrdinalIgnoreCase)) return Prioridades.Media;
        if (string.Equals(trimmed, Prioridades.Baixa, StringComparison.OrdinalIgnoreCase)) return Prioridades.Baixa;

        throw new DomainException("Prioridade invalida. Use Alta, Media ou Baixa.");
    }

    private static string NormalizarStatus(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Statuses.Pendente;

        var trimmed = value.Trim();
        if (string.Equals(trimmed, Statuses.Pendente, StringComparison.OrdinalIgnoreCase)) return Statuses.Pendente;
        if (string.Equals(trimmed, Statuses.EmAndamento, StringComparison.OrdinalIgnoreCase)) return Statuses.EmAndamento;
        if (string.Equals(trimmed, Statuses.Concluido, StringComparison.OrdinalIgnoreCase)) return Statuses.Concluido;
        if (string.Equals(trimmed, Statuses.Cancelado, StringComparison.OrdinalIgnoreCase)) return Statuses.Cancelado;

        throw new DomainException("Status invalido. Use Pendente, Em andamento, Concluido ou Cancelado.");
    }

    private static int? ValidarLembreteMinutos(int? value)
    {
        if (!value.HasValue)
            return null;

        if (value.Value < 0 || value.Value > 10080)
            throw new DomainException("Lembrete em minutos deve estar entre 0 e 10080.");

        return value.Value;
    }

    private static string? ValidarCor(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        if (!CorHexRegex.IsMatch(trimmed))
            throw new DomainException("Cor invalida. Use formato hexadecimal como #1F2A35.");

        return trimmed.ToUpperInvariant();
    }
}
