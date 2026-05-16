import type { EventoCalendario } from "@/lib/api/calendario";
import type { HorarioFuncionamentoDto } from "@/lib/api/configuracoes";
import { duracaoAgendaMinutos } from "./ocupacao-agenda";

/** Mesma grade da Agenda Global (8h–21h, 80px/hora). */
export const AGENDA_HORAS_DIA = [8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21];
export const AGENDA_ALTURA_HORA_PX = 80;
export const AGENDA_HORA_INICIO_GRID = 8;
export const AGENDA_GRID_ALTURA_PX = AGENDA_HORAS_DIA.length * AGENDA_ALTURA_HORA_PX;

export function horarioParaTopPxAgenda(horario: string): number {
  const [h, m] = horario.split(":").map(Number);
  return ((h - AGENDA_HORA_INICIO_GRID) * 60 + m) * (AGENDA_ALTURA_HORA_PX / 60);
}

export function alturaCardPxAgenda(inicio: string, fim: string, alturaMinima: number): number {
  const minutos = duracaoAgendaMinutos(inicio, fim);
  const alturaReal = (minutos / 60) * AGENDA_ALTURA_HORA_PX;
  return Math.max(alturaReal, alturaMinima);
}

export function faixasForaDoHorarioEscola(
  abertura: string,
  fechamento: string,
): { topPx: number; heightPx: number }[] {
  const out: { topPx: number; heightPx: number }[] = [];
  const topAb = horarioParaTopPxAgenda(abertura);
  const topFe = horarioParaTopPxAgenda(fechamento);
  if (topAb > 2) {
    const h = Math.min(topAb, AGENDA_GRID_ALTURA_PX);
    if (h >= 8) out.push({ topPx: 0, heightPx: h });
  }
  if (topFe < AGENDA_GRID_ALTURA_PX - 2) {
    const h = AGENDA_GRID_ALTURA_PX - topFe;
    if (h >= 8) out.push({ topPx: topFe, heightPx: h });
  }
  return out;
}

export function corEventoCalendario(tipoEvento: EventoCalendario["tipoEvento"]): string {
  if (tipoEvento === "FERIADO") return "bg-red-50 text-red-700 border-red-200";
  if (tipoEvento === "RECESSO") return "bg-blue-50 text-blue-700 border-blue-200";
  return "bg-amber-50 text-amber-700 border-amber-200";
}

export type EstadoDiaAgendaColuna = {
  dataIso: string;
  eventoCalendario: EventoCalendario | null;
  escolaFechadaDiaInteiro: boolean;
  faixasForaExpediente: { topPx: number; heightPx: number }[];
};

export function calcularEstadoDiaAgendaColuna(
  dataIso: string,
  eventosCalendario: EventoCalendario[],
  horariosFuncionamento: HorarioFuncionamentoDto[],
): EstadoDiaAgendaColuna {
  const eventoCalendario =
    eventosCalendario.find((e) => e.dataEvento === dataIso && e.suspendeAula) ?? null;

  const diaSemana = new Date(`${dataIso}T12:00:00`).getDay();
  const configHorarioDia = horariosFuncionamento.find((h) => h.diaSemana === diaSemana);

  const escolaFechadaDiaInteiro =
    !eventoCalendario && configHorarioDia != null && !configHorarioDia.aberto;

  let faixasForaExpediente: { topPx: number; heightPx: number }[] = [];
  if (
    !eventoCalendario &&
    !escolaFechadaDiaInteiro &&
    configHorarioDia?.aberto &&
    configHorarioDia.horarioAbertura &&
    configHorarioDia.horarioFechamento
  ) {
    faixasForaExpediente = faixasForaDoHorarioEscola(
      configHorarioDia.horarioAbertura,
      configHorarioDia.horarioFechamento,
    );
  }

  return {
    dataIso,
    eventoCalendario,
    escolaFechadaDiaInteiro,
    faixasForaExpediente,
  };
}

export async function listarEventosParaDatas(datas: Date[]): Promise<EventoCalendario[]> {
  const { listarEventos } = await import("@/lib/api/calendario");
  const chaves = new Set<string>();
  const promises: Promise<EventoCalendario[]>[] = [];

  for (const d of datas) {
    const mes = d.getMonth() + 1;
    const ano = d.getFullYear();
    const key = `${ano}-${mes}`;
    if (!chaves.has(key)) {
      chaves.add(key);
      promises.push(listarEventos(mes, ano));
    }
  }

  if (promises.length === 0) return [];
  const arrays = await Promise.all(promises);
  return arrays.flat();
}
