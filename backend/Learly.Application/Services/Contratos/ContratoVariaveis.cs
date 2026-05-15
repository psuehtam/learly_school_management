using System.Globalization;
using System.Text.RegularExpressions;
using Learly.Domain.Entities;
using Learly.Domain.ReadModels;

namespace Learly.Application.Services.Contratos;

/// <summary>
/// Define e substitui todas as variáveis suportadas nos templates de contrato.
/// Variáveis no formato <c>{{Namespace_Campo}}</c>.
/// </summary>
internal static class ContratoVariaveis
{
    public static readonly IReadOnlyList<(string Variavel, string Descricao)> Lista =
    [
        ("{{PreAluno_NomeCompleto}}",      "Nome completo do pré-aluno"),
        ("{{PreAluno_Nome}}",              "Primeiro nome do pré-aluno"),
        ("{{PreAluno_Sobrenome}}",         "Sobrenome do pré-aluno"),
        ("{{PreAluno_DataNascimento}}",    "Data de nascimento (dd/MM/yyyy)"),
        ("{{PreAluno_Telefone}}",          "Telefone do pré-aluno"),
        ("{{PreAluno_TipoContrato}}",      "Tipo/duração do contrato (ex.: 12 meses)"),
        ("{{PreAluno_ValorMensalidade}}",  "Valor da mensalidade (R$)"),
        ("{{PreAluno_ValorMatricula}}",    "Valor da matrícula (R$)"),
        ("{{PreAluno_ValorMaterial}}",     "Valor do material/livro (R$)"),
        ("{{PreAluno_FormaPagamento}}",    "Forma de pagamento da mensalidade"),
        ("{{Responsavel_NomeCompleto}}",   "Nome completo do responsável financeiro"),
        ("{{Responsavel_Nome}}",           "Primeiro nome do responsável"),
        ("{{Responsavel_Sobrenome}}",      "Sobrenome do responsável"),
        ("{{Responsavel_CpfCnpj}}",        "CPF ou CNPJ do responsável"),
        ("{{Responsavel_TipoPessoa}}",     "Tipo: Fisica ou Juridica"),
        ("{{Escola_Nome}}",                "Nome fantasia da escola"),
        ("{{Escola_Codigo}}",              "Código da escola"),
        ("{{Data_Atual}}",                 "Data atual (dd/MM/yyyy)"),
        ("{{Data_Atual_Extenso}}",         "Data atual por extenso (ex.: 14 de maio de 2026)"),
        ("{{Data_Atual_Ano}}",             "Ano atual"),
    ];

    /// <summary>
    /// Substitui variáveis usando o read model <see cref="PreAlunoDetalheItem"/>,
    /// que já carrega os dados do responsável sem precisar de uma query adicional.
    /// </summary>
    public static string SubstituirDetalhe(
        string template,
        PreAlunoDetalheItem detalhe,
        Escola escola)
    {
        var ptBr = new CultureInfo("pt-BR");
        var hoje = DateTime.Today;

        // Extrai primeiro nome / sobrenome do campo composto do responsável
        var nomeRespParts = detalhe.ResponsavelNomeCompleto.Trim().Split(' ', 2);
        var nomeResp = nomeRespParts[0];
        var sobrenomeResp = nomeRespParts.Length > 1 ? nomeRespParts[1] : string.Empty;

        var mapa = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["{{PreAluno_NomeCompleto}}"]      = $"{detalhe.NomeAluno.Trim()} {detalhe.SobrenomeAluno.Trim()}".Trim(),
            ["{{PreAluno_Nome}}"]              = detalhe.NomeAluno.Trim(),
            ["{{PreAluno_Sobrenome}}"]         = detalhe.SobrenomeAluno.Trim(),
            ["{{PreAluno_DataNascimento}}"]    = detalhe.DataNascimentoAluno.ToString("dd/MM/yyyy"),
            ["{{PreAluno_Telefone}}"]          = detalhe.TelefoneAluno ?? string.Empty,
            ["{{PreAluno_TipoContrato}}"]      = detalhe.TipoContrato,
            ["{{PreAluno_ValorMensalidade}}"]  = detalhe.ValorMensalidade.ToString("C", ptBr),
            ["{{PreAluno_ValorMatricula}}"]    = detalhe.ValorMatricula.ToString("C", ptBr),
            ["{{PreAluno_ValorMaterial}}"]     = (detalhe.ValorMaterial ?? 0).ToString("C", ptBr),
            ["{{PreAluno_FormaPagamento}}"]    = detalhe.FormaPagamento ?? string.Empty,

            ["{{Responsavel_NomeCompleto}}"]  = detalhe.ResponsavelNomeCompleto,
            ["{{Responsavel_Nome}}"]          = nomeResp,
            ["{{Responsavel_Sobrenome}}"]     = sobrenomeResp,
            ["{{Responsavel_CpfCnpj}}"]       = detalhe.ResponsavelCpfCnpj,
            ["{{Responsavel_TipoPessoa}}"]    = detalhe.ResponsavelTipoPessoa,

            ["{{Escola_Nome}}"]    = escola.NomeFantasia,
            ["{{Escola_Codigo}}"]  = escola.CodigoEscola,

            ["{{Data_Atual}}"]         = hoje.ToString("dd/MM/yyyy"),
            ["{{Data_Atual_Extenso}}"] = hoje.ToString("d 'de' MMMM 'de' yyyy", ptBr),
            ["{{Data_Atual_Ano}}"]     = hoje.Year.ToString(),
        };

        return Regex.Replace(
            template,
            @"\{\{[A-Za-z0-9_]+\}\}",
            m => mapa.TryGetValue(m.Value, out var v) ? v : m.Value);
    }
}
