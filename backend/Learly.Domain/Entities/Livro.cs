using Learly.Domain.Exceptions;

namespace Learly.Domain.Entities;

/// <summary>Catálogo de livros (níveis) por escola — mapeamento mínimo para matrícula e pré-alunos.</summary>
public sealed class Livro
{
    public int Id { get; internal set; }
    public int EscolaId { get; internal set; }

    private string _nome = string.Empty;
    public string Nome
    {
        get => _nome;
        internal set => _nome = ValidarNome(value);
    }

    /// <summary>Valores esperados no banco: <c>Ativo</c> ou <c>Inativo</c>.</summary>
    private string _status = "Ativo";
    public string Status
    {
        get => _status;
        internal set =>
            _status = ValidarStatus(value);
    }

    public DateTime DataCriacao { get; internal set; }
    public DateTime DataAtualizacao { get; internal set; }

    public ICollection<Capitulo> Capitulos { get; internal set; } = new List<Capitulo>();

    private static string ValidarNome(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Nome do livro e obrigatorio.");

        var trimmed = value.Trim();
        if (trimmed.Length > 150)
            throw new DomainException("Nome do livro deve ter ate 150 caracteres.");

        return trimmed;
    }

    private static string ValidarStatus(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Ativo";

        var trimmed = value.Trim();
        if (string.Equals(trimmed, "Ativo", StringComparison.OrdinalIgnoreCase)) return "Ativo";
        if (string.Equals(trimmed, "Inativo", StringComparison.OrdinalIgnoreCase)) return "Inativo";

        throw new DomainException("Status do livro deve ser Ativo ou Inativo.");
    }
}
