import type { Compromisso } from "@/lib/api/compromissos";
import type { Aula } from "@/types/aula";
import {
  mapearAulasParaOcupacoesAgenda,
  mapearCompromissosParaOcupacoesAgenda,
  type OcupacaoAgendaItem,
} from "./ocupacao-agenda";

export function dataParaIsoLocal(d: Date): string {
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, "0");
  const day = String(d.getDate()).padStart(2, "0");
  return `${y}-${m}-${day}`;
}

export function getInicioSemanaSegunda(data: Date): Date {
  const d = new Date(data);
  const dia = d.getDay();
  const diff = dia === 0 ? -6 : 1 - dia;
  d.setDate(d.getDate() + diff);
  d.setHours(0, 0, 0, 0);
  return d;
}

/** Segunda a domingo (7 colunas na grade semanal). */
export function diasDaSemana(inicioSegunda: Date): Date[] {
  return Array.from({ length: 7 }, (_, i) => {
    const d = new Date(inicioSegunda);
    d.setDate(inicioSegunda.getDate() + i);
    return d;
  });
}

function compromissoNoDiaLocal(c: Compromisso, dataIso: string): boolean {
  if (c.status === "Cancelado") return false;
  const ini = new Date(c.dataInicio);
  const fim = new Date(c.dataFim);
  const alvo = new Date(`${dataIso}T12:00:00`);
  const inicioDia = new Date(alvo);
  inicioDia.setHours(0, 0, 0, 0);
  const fimDia = new Date(alvo);
  fimDia.setHours(23, 59, 59, 999);
  return ini <= fimDia && fim >= inicioDia;
}

/** Itens de aula + compromisso do usuário em um dia (grade semanal). */
export function itensMinhaAgendaNoDia(
  dataIso: string,
  aulas: Aula[],
  compromissos: Compromisso[],
  usuarioId: number,
): OcupacaoAgendaItem[] {
  const aulasDia = mapearAulasParaOcupacoesAgenda(aulas, dataIso, {
    professorId: usuarioId,
  });
  const compsDoDia = compromissos.filter((c) => compromissoNoDiaLocal(c, dataIso));
  const comps = mapearCompromissosParaOcupacoesAgenda(compsDoDia, {
    usuarioId,
  });
  return [...aulasDia, ...comps].sort((a, b) => a.inicio.localeCompare(b.inicio));
}

export function parseAulaIdDeOcupacao(item: OcupacaoAgendaItem): number | null {
  if (item.tipo === "COMPROMISSO") return null;
  const m = /^aula-(\d+)$/.exec(item.id);
  return m ? Number.parseInt(m[1], 10) : null;
}
