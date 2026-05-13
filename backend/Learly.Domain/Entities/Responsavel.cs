namespace Learly.Domain.Entities;

/// <summary>Leitura e relatórios; inserções de endereço completo podem usar SQL na infraestrutura.</summary>
public sealed class Responsavel
{
    public int Id { get; internal set; }
    public int EscolaId { get; internal set; }

    public string TipoPessoa { get; internal set; } = "Fisica";
    public string CpfCnpj { get; internal set; } = string.Empty;
    public string Nome { get; internal set; } = string.Empty;
    public string Sobrenome { get; internal set; } = string.Empty;
    public string Status { get; internal set; } = "Ativo";
}
