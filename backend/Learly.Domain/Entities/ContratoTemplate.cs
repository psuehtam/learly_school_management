using Learly.Domain.Exceptions;

namespace Learly.Domain.Entities;

public sealed class ContratoTemplate
{
    public int Id { get; internal set; }
    public int EscolaId { get; internal set; }

    private string _nome = string.Empty;
    public string Nome
    {
        get => _nome;
        internal set => _nome = string.IsNullOrWhiteSpace(value)
            ? throw new DomainException("Nome do template e obrigatorio.")
            : value.Trim();
    }

    private string _conteudoHtml = string.Empty;
    public string ConteudoHtml
    {
        get => _conteudoHtml;
        internal set => _conteudoHtml = string.IsNullOrWhiteSpace(value)
            ? throw new DomainException("Conteudo do template e obrigatorio.")
            : value;
    }

    public int Versao { get; internal set; }
    public bool Ativo { get; internal set; }
    public int CriadoPorUsuarioId { get; internal set; }
    public DateTime DataCriacao { get; internal set; }
}
