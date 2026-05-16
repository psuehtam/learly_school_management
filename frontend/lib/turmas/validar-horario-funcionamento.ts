import type { HorarioFuncionamentoDto } from "@/lib/api/configuracoes";

const NOMES_DIA = [
  "domingo",
  "segunda-feira",
  "terça-feira",
  "quarta-feira",
  "quinta-feira",
  "sexta-feira",
  "sábado",
];

function minutosDeHhMm(value: string): number {
  const [h, m] = value.split(":").map((x) => Number.parseInt(x, 10));
  return (h ?? 0) * 60 + (m ?? 0);
}

/**
 * Valida se o intervalo da turma cabe no horário de funcionamento da escola
 * para cada dia da semana selecionado (0=dom … 6=sáb).
 * Retorna mensagem de erro ou null se ok / nada a validar.
 */
export function validarHorarioTurmaFuncionamento(
  dias: number[],
  horaIni: string,
  horaFim: string,
  grade: HorarioFuncionamentoDto[],
): string | null {
  if (dias.length === 0 || !horaIni.trim() || !horaFim.trim()) {
    return null;
  }

  if (grade.length === 0) {
    return "Horário de funcionamento da escola não configurado. Cadastre em Configurações da escola.";
  }

  const ini = minutosDeHhMm(horaIni);
  const fim = minutosDeHhMm(horaFim);
  if (fim <= ini) {
    return "Horário de término deve ser maior que o de início.";
  }

  for (const dia of dias) {
    const config = grade.find((g) => g.diaSemana === dia);
    if (!config?.aberto || !config.horarioAbertura || !config.horarioFechamento) {
      return `A escola não funciona em ${NOMES_DIA[dia] ?? `dia ${dia}`}. Escolha outro dia.`;
    }

    const abertura = minutosDeHhMm(config.horarioAbertura);
    const fechamento = minutosDeHhMm(config.horarioFechamento);

    if (ini < abertura || fim > fechamento) {
      return `Horário fora do funcionamento em ${NOMES_DIA[dia]} (permitido: ${config.horarioAbertura} – ${config.horarioFechamento}).`;
    }
  }

  return null;
}
