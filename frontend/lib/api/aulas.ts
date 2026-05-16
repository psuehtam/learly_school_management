import { apiRequest } from "@/lib/api/client";
import type { Aula, Presenca, Homework, Avaliacao } from "@/types/aula";

export type { Aula, Presenca, Homework, Avaliacao } from "@/types/aula";

function normalizarAula(raw: Aula & Record<string, unknown>): Aula {
  const r = raw as Aula & {
    DataAula?: string;
    HorarioInicio?: string;
    HorarioFim?: string;
    TurmaNome?: string;
    LivroNome?: string;
    TipoAula?: string;
    Status?: string;
    ProfessorId?: number;
    TurmaId?: number;
    NumeroAula?: number;
  };
  return {
    ...raw,
    turmaId: raw.turmaId ?? r.TurmaId ?? 0,
    professorId: raw.professorId ?? r.ProfessorId ?? 0,
    numeroAula: raw.numeroAula ?? r.NumeroAula ?? 0,
    dataAula: (raw.dataAula ?? r.DataAula ?? "").toString().slice(0, 10),
    horarioInicio: String(raw.horarioInicio ?? r.HorarioInicio ?? "").slice(0, 5),
    horarioFim: String(raw.horarioFim ?? r.HorarioFim ?? "").slice(0, 5),
    tipoAula: (raw.tipoAula ?? r.TipoAula ?? "Normal") as Aula["tipoAula"],
    status: (raw.status ?? r.Status ?? "Agendada") as Aula["status"],
    turmaNome: raw.turmaNome ?? r.TurmaNome,
    livroNome: raw.livroNome ?? r.LivroNome,
    reposicaoAlunoNome: raw.reposicaoAlunoNome ?? (r as { ReposicaoAlunoNome?: string }).ReposicaoAlunoNome,
    reposicaoAulaOriginalNumero:
      raw.reposicaoAulaOriginalNumero ?? (r as { ReposicaoAulaOriginalNumero?: number }).ReposicaoAulaOriginalNumero,
    reposicaoAulaOriginalData:
      raw.reposicaoAulaOriginalData ?? (r as { ReposicaoAulaOriginalData?: string }).ReposicaoAulaOriginalData,
  };
}

export async function listarAulas(filtros?: Record<string, string>): Promise<Aula[]> {
  const params = filtros ? `?${new URLSearchParams(filtros)}` : "";
  const data = await apiRequest<Aula[]>(`/api/aulas${params}`);
  return data.map((a) => normalizarAula(a as Aula & Record<string, unknown>));
}

export async function buscarAula(id: number): Promise<Aula> {
  return apiRequest<Aula>(`/api/aulas/${id}`);
}

export async function criarAula(dados: Partial<Aula>): Promise<{ id: number }> {
  return apiRequest<{ id: number }>("/api/aulas", { method: "POST", body: dados });
}

export async function realizarAula(id: number): Promise<void> {
  await apiRequest<void>(`/api/aulas/${id}/realizar`, { method: "PATCH" });
}

export async function registrarConteudo(id: number, conteudo: string): Promise<void> {
  await apiRequest<void>(`/api/aulas/${id}/conteudo`, {
    method: "PATCH",
    body: { conteudo },
  });
}

export async function cancelarAula(id: number): Promise<void> {
  await apiRequest<void>(`/api/aulas/${id}`, { method: "DELETE" });
}

export async function registrarChamada(
  aulaId: number,
  presencas: { alunoId: number; status: string }[],
): Promise<void> {
  await apiRequest<void>(`/api/aulas/${aulaId}/chamada`, {
    method: "POST",
    body: { presencas },
  });
}

export async function listarPresencas(aulaId: number): Promise<Presenca[]> {
  return apiRequest<Presenca[]>(`/api/aulas/${aulaId}/presencas`);
}

export async function lancarHomework(
  aulaId: number,
  notas: { alunoId: number; nota?: number; observacao?: string }[],
): Promise<void> {
  await apiRequest<void>(`/api/aulas/${aulaId}/homework`, {
    method: "POST",
    body: { notas },
  });
}

export async function listarHomework(aulaId: number): Promise<Homework[]> {
  return apiRequest<Homework[]>(`/api/aulas/${aulaId}/homework`);
}

export async function lancarAvaliacao(
  turmaId: number,
  alunoId: number,
  dados: Partial<Avaliacao>,
): Promise<Avaliacao> {
  return apiRequest<Avaliacao>(`/api/turmas/${turmaId}/avaliacoes`, {
    method: "POST",
    body: { ...dados, alunoId },
  });
}
