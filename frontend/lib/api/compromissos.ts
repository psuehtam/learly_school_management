import { apiRequest } from "@/lib/api/client";

export interface Compromisso {
  id: number;
  titulo: string;
  descricao?: string;
  dataInicio: string;
  dataFim: string;
  local?: string;
  tipo: string;
  prioridade: string;
  status: string;
  lembreteMinutos?: number;
  cor?: string;
  participantesUsuarioIds: number[];
}

export interface CriarCompromissoPayload {
  titulo: string;
  descricao?: string;
  dataInicio: string;
  dataFim: string;
  local?: string;
  tipo?: string;
  prioridade?: string;
  lembreteMinutos?: number;
  cor?: string;
  participantesUsuarioIds: number[];
}

export interface ParticipanteCompromisso {
  id: number;
  nomeCompleto: string;
  perfilNome: string;
}

export async function listarCompromissos(): Promise<Compromisso[]> {
  return apiRequest<Compromisso[]>("/api/compromissos");
}

export async function listarCompromissosAgendaGlobal(data: string, usuarioId?: number): Promise<Compromisso[]> {
  const params = new URLSearchParams({ data });
  if (usuarioId) params.set("usuarioId", String(usuarioId));
  return apiRequest<Compromisso[]>(`/api/compromissos/agenda-global?${params.toString()}`);
}

export async function listarParticipantesCompromissos(): Promise<ParticipanteCompromisso[]> {
  return apiRequest<ParticipanteCompromisso[]>("/api/compromissos/participantes");
}

export async function criarCompromisso(payload: CriarCompromissoPayload): Promise<Compromisso> {
  return apiRequest<Compromisso>("/api/compromissos", { method: "POST", body: payload });
}

export async function editarCompromisso(id: number, payload: Partial<CriarCompromissoPayload>): Promise<Compromisso> {
  return apiRequest<Compromisso>(`/api/compromissos/${id}`, { method: "PUT", body: payload });
}

export async function cancelarCompromisso(id: number, motivo: string): Promise<void> {
  await apiRequest<void>(`/api/compromissos/${id}/cancelar`, {
    method: "POST",
    body: { motivo },
  });
}
