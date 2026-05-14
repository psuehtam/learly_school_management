namespace Learly.Application.Contracts.HorariosFuncionamento;

public enum HorariosFuncionamentoFalha
{
    Nenhuma,
    AcessoNegado,
    Validacao,
}

public sealed record HorariosFuncionamentoListagemResultado(
    bool Ok,
    IReadOnlyList<HorarioFuncionamentoResponse> Itens,
    string? Mensagem,
    HorariosFuncionamentoFalha Falha);

public sealed record HorariosFuncionamentoAtualizacaoResultado(
    bool Ok,
    IReadOnlyList<HorarioFuncionamentoResponse>? Itens,
    string? Mensagem,
    HorariosFuncionamentoFalha Falha);
