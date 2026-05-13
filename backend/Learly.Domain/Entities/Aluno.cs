using Learly.Domain.Exceptions;

namespace Learly.Domain.Entities;

public sealed class Aluno
{
    public static class Estados
    {
        public const string Ativo = "Ativo";
        public const string Inativo = "Inativo";
        public const string Trancado = "Trancado";

        public static string Normalize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new DomainException("Status do aluno e obrigatorio.");

            var s = value.Trim();
            if (s.Equals(Ativo, StringComparison.OrdinalIgnoreCase)) return Ativo;
            if (s.Equals(Inativo, StringComparison.OrdinalIgnoreCase)) return Inativo;
            if (s.Equals(Trancado, StringComparison.OrdinalIgnoreCase)) return Trancado;

            throw new DomainException("Status do aluno invalido.");
        }
    }

    public int Id { get; internal set; }
    public int EscolaId { get; internal set; }
    public int ResponsavelId { get; internal set; }
    public bool EProprioResponsavel { get; internal set; }

    private string _nome = string.Empty;
    public string Nome
    {
        get => _nome;
        internal set => _nome = ValidarObrigatorio(value, "Nome do aluno e obrigatorio.");
    }

    private string _sobrenome = string.Empty;
    public string Sobrenome
    {
        get => _sobrenome;
        internal set => _sobrenome = ValidarObrigatorio(value, "Sobrenome do aluno e obrigatorio.");
    }

    private string _sexo = string.Empty;
    public string Sexo
    {
        get => _sexo;
        internal set => _sexo = NormalizarSexo(value);
    }

    public DateOnly DataNascimento { get; internal set; }
    public DateOnly DataIngresso { get; internal set; }

    private string? _cpf;
    public string? Cpf
    {
        get => _cpf;
        internal set => _cpf = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private string _cep = string.Empty;
    public string Cep
    {
        get => _cep;
        internal set => _cep = ValidarObrigatorio(value, "CEP do aluno e obrigatorio.");
    }

    private string _tipoLogradouro = string.Empty;
    public string TipoLogradouro
    {
        get => _tipoLogradouro;
        internal set => _tipoLogradouro = ValidarObrigatorio(value, "Tipo de logradouro do aluno e obrigatorio.");
    }

    private string _logradouro = string.Empty;
    public string Logradouro
    {
        get => _logradouro;
        internal set => _logradouro = ValidarObrigatorio(value, "Logradouro do aluno e obrigatorio.");
    }

    private string _numero = string.Empty;
    public string Numero
    {
        get => _numero;
        internal set => _numero = ValidarObrigatorio(value, "Numero do aluno e obrigatorio.");
    }

    private string? _complemento;
    public string? Complemento
    {
        get => _complemento;
        internal set => _complemento = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private string _bairro = string.Empty;
    public string Bairro
    {
        get => _bairro;
        internal set => _bairro = ValidarObrigatorio(value, "Bairro do aluno e obrigatorio.");
    }

    private string _municipio = string.Empty;
    public string Municipio
    {
        get => _municipio;
        internal set => _municipio = ValidarObrigatorio(value, "Municipio do aluno e obrigatorio.");
    }

    public string? CorRaca { get; internal set; }
    public string? EstadoCivil { get; internal set; }
    public string? Profissao { get; internal set; }
    public string? RegistroEscolar { get; internal set; }
    public string? Nacionalidade { get; internal set; }
    public DateOnly? DataEntradaPais { get; internal set; }
    public string? NaturalidadeCidade { get; internal set; }
    public string? NaturalidadeEstado { get; internal set; }
    public string? RgNumero { get; internal set; }
    public DateOnly? RgExpedicao { get; internal set; }
    public string? RgOrgao { get; internal set; }

    private string _status = Estados.Ativo;
    public string Status
    {
        get => _status;
        internal set => _status = Estados.Normalize(value);
    }

    public DateTime DataCriacao { get; internal set; }
    public DateTime DataAtualizacao { get; internal set; }

    private static string ValidarObrigatorio(string? value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException(message);

        return value.Trim();
    }

    private static string NormalizarSexo(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Sexo do aluno e obrigatorio.");

        var s = value.Trim();
        if (s.Equals("Masculino", StringComparison.OrdinalIgnoreCase)) return "Masculino";
        if (s.Equals("Feminino", StringComparison.OrdinalIgnoreCase)) return "Feminino";
        if (s.Equals("Outro", StringComparison.OrdinalIgnoreCase)) return "Outro";

        throw new DomainException("Sexo do aluno invalido.");
    }
}
