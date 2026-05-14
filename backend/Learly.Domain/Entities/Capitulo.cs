using Learly.Domain.Exceptions;

namespace Learly.Domain.Entities;

/// <summary>Capítulo de um livro — tabela <c>capitulos</c> (<c>qtd_aulas_previstas</c>).</summary>
public sealed class Capitulo
{
    public int Id { get; internal set; }
    public int EscolaId { get; internal set; }
    public int LivroId { get; internal set; }

    private string _nome = string.Empty;
    public string Nome
    {
        get => _nome;
        internal set => _nome = ValidarNome(value);
    }

    private int _qtdAulasPrevistas;
    public int QtdAulasPrevistas
    {
        get => _qtdAulasPrevistas;
        internal set => _qtdAulasPrevistas = ValidarQtdAulasPrevistas(value);
    }

    private string _status = "Ativo";
    public string Status
    {
        get => _status;
        internal set =>
            _status = ValidarStatus(value);
    }

    public DateTime DataCriacao { get; internal set; }
    public DateTime DataAtualizacao { get; internal set; }

    public Livro Livro { get; internal set; } = null!;

    private static string ValidarNome(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Nome do capitulo e obrigatorio.");

        var trimmed = value.Trim();
        if (trimmed.Length > 100)
            throw new DomainException("Nome do capitulo deve ter ate 100 caracteres.");

        return trimmed;
    }

    private static int ValidarQtdAulasPrevistas(int value)
    {
        if (value <= 0)
            throw new DomainException("Quantidade de aulas previstas deve ser maior que zero.");

        return value;
    }

    private static string ValidarStatus(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Ativo";

        var trimmed = value.Trim();
        if (string.Equals(trimmed, "Ativo", StringComparison.OrdinalIgnoreCase)) return "Ativo";
        if (string.Equals(trimmed, "Inativo", StringComparison.OrdinalIgnoreCase)) return "Inativo";

        throw new DomainException("Status do capitulo deve ser Ativo ou Inativo.");
    }
}
