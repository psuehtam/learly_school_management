namespace Learly.Domain.Entities;

public sealed class ContratoGerado
{
    public int Id { get; internal set; }
    public int EscolaId { get; internal set; }
    public int PreAlunoId { get; internal set; }
    public int TemplateId { get; internal set; }
    public string ConteudoGeradoHtml { get; internal set; } = string.Empty;
    public int GeradoPorUsuarioId { get; internal set; }
    public DateTime DataGeracao { get; internal set; }
}
