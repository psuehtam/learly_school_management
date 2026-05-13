using Learly.Domain.Exceptions;

namespace Learly.Domain.Entities;

/// <summary>Configuração de funcionamento semanal da escola.</summary>
public sealed class EscolaHorarioFuncionamento
{
    public int Id { get; internal set; }
    public int EscolaId { get; internal set; }

    private int _diaSemana;
    /// <summary>Dia da semana no padrão .NET: 0=Domingo ... 6=Sábado.</summary>
    public int DiaSemana
    {
        get => _diaSemana;
        internal set => _diaSemana = ValidarDiaSemana(value);
    }

    public bool Aberto { get; internal set; }
    public TimeOnly? HorarioAbertura { get; internal set; }
    public TimeOnly? HorarioFechamento { get; internal set; }

    public bool PermiteIntervalo(TimeOnly inicio, TimeOnly fim)
    {
        if (!Aberto)
            return false;

        if (!HorarioAbertura.HasValue || !HorarioFechamento.HasValue)
            return false;

        return inicio >= HorarioAbertura.Value && fim <= HorarioFechamento.Value && fim > inicio;
    }

    public void ValidarConfiguracao()
    {
        if (!Aberto)
            return;

        if (!HorarioAbertura.HasValue || !HorarioFechamento.HasValue)
            throw new DomainException("Horario de abertura e fechamento sao obrigatorios para dia aberto.");

        if (HorarioFechamento.Value <= HorarioAbertura.Value)
            throw new DomainException("Horario de fechamento deve ser maior que horario de abertura.");
    }

    public void Atualizar(bool aberto, TimeOnly? horarioAbertura, TimeOnly? horarioFechamento)
    {
        Aberto = aberto;
        HorarioAbertura = aberto ? horarioAbertura : null;
        HorarioFechamento = aberto ? horarioFechamento : null;
        ValidarConfiguracao();
    }

    public static EscolaHorarioFuncionamento Criar(
        int escolaId,
        int diaSemana,
        bool aberto,
        TimeOnly? horarioAbertura,
        TimeOnly? horarioFechamento)
    {
        var h = new EscolaHorarioFuncionamento
        {
            EscolaId = escolaId,
            DiaSemana = diaSemana,
            Aberto = aberto,
            HorarioAbertura = aberto ? horarioAbertura : null,
            HorarioFechamento = aberto ? horarioFechamento : null,
        };
        h.ValidarConfiguracao();
        return h;
    }

    private static int ValidarDiaSemana(int value)
    {
        if (value < 0 || value > 6)
            throw new DomainException("Dia da semana invalido. Use 0 (domingo) a 6 (sabado).");

        return value;
    }
}
