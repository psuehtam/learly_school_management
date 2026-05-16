using Learly.Domain.Entities;

namespace Learly.Application.Services.Turmas;

/// <summary>Gera datas de aula e distribui capítulos conforme documentação §6.3.4.</summary>
internal static class TurmaAulaGerador
{
    public sealed record AulaPlanejada(
        int CapituloId,
        int NumeroAula,
        DateOnly DataAula);

    public sealed record ResultadoGeracao(
        IReadOnlyList<AulaPlanejada> Aulas,
        DateOnly? DataTerminoPrevista);

    public static ResultadoGeracao Gerar(
        DateOnly dataInicio,
        IReadOnlyList<int> diasSemana,
        IReadOnlyList<Capitulo> capitulosOrdenados,
        Func<DateOnly, bool> diaSuspendeAula,
        int maxDiasVarredura = 365 * 3)
    {
        var slotsNecessarios = capitulosOrdenados.Sum(c => c.QtdAulasPrevistas);
        if (slotsNecessarios == 0)
        {
            return new ResultadoGeracao([], null);
        }

        var diasSet = new HashSet<int>(diasSemana);
        var datasValidas = new List<DateOnly>();
        var cursor = dataInicio;

        for (var i = 0; i < maxDiasVarredura && datasValidas.Count < slotsNecessarios; i++)
        {
            var dow = (int)cursor.DayOfWeek;
            if (diasSet.Contains(dow) && !diaSuspendeAula(cursor))
            {
                datasValidas.Add(cursor);
            }

            cursor = cursor.AddDays(1);
        }

        if (datasValidas.Count < slotsNecessarios)
        {
            throw new InvalidOperationException(
                $"Nao foi possivel alocar {slotsNecessarios} aulas em {maxDiasVarredura} dias (encontradas {datasValidas.Count} datas validas).");
        }

        var aulas = new List<AulaPlanejada>(slotsNecessarios);
        var indiceData = 0;
        var numeroAula = 1;

        foreach (var cap in capitulosOrdenados)
        {
            for (var n = 0; n < cap.QtdAulasPrevistas; n++)
            {
                aulas.Add(new AulaPlanejada(cap.Id, numeroAula++, datasValidas[indiceData++]));
            }
        }

        var termino = aulas.Count > 0 ? aulas[^1].DataAula : (DateOnly?)null;
        return new ResultadoGeracao(aulas, termino);
    }

    public static string MontarNomeTurma(string livroNome, int codigoSequencial, IReadOnlyList<int> diasSemana, TimeOnly horarioInicio)
    {
        var abrevs = diasSemana
            .Distinct()
            .OrderBy(d => d)
            .Select(DiaSemanaAbrev)
            .Where(s => s.Length > 0);
        var diasStr = string.Join("-", abrevs);
        var cod = codigoSequencial.ToString("00", System.Globalization.CultureInfo.InvariantCulture);
        var hora = horarioInicio.ToString("HH:mm", System.Globalization.CultureInfo.InvariantCulture);
        return $"{livroNome.Trim().ToUpperInvariant()} / {cod} {diasStr} {hora}";
    }

    private static string DiaSemanaAbrev(int dia) => dia switch
    {
        0 => "DOM",
        1 => "SEG",
        2 => "TER",
        3 => "QUA",
        4 => "QUI",
        5 => "SEX",
        6 => "SAB",
        _ => ""
    };
}
