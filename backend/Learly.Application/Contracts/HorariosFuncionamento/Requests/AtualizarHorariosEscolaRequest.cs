namespace Learly.Application.Contracts.HorariosFuncionamento.Requests;

public sealed class AtualizarHorariosEscolaRequest
{
    /// <summary>Lista de configurações por dia. Não é necessário enviar todos os 7 dias.</summary>
    public IList<HorarioDiaSemanaItem> Dias { get; set; } = [];
}

public sealed class HorarioDiaSemanaItem
{
    /// <summary>0=Domingo … 6=Sábado.</summary>
    public int DiaSemana { get; set; }

    public bool Aberto { get; set; }

    /// <summary>Obrigatório quando <see cref="Aberto"/> = true. Formato "HH:mm".</summary>
    public string? HorarioAbertura { get; set; }

    /// <summary>Obrigatório quando <see cref="Aberto"/> = true. Formato "HH:mm".</summary>
    public string? HorarioFechamento { get; set; }
}
