import { apiRequest } from "@/lib/api/client";

export type MatriculaStatus = "Em Espera" | "Ativo" | "Concluido" | "Trancado" | "Cancelado";

export interface MatriculaListItem {
  id: number;
  escolaId: number;
  alunoId: number;
  turmaId: number | null;
  dataMatricula: string;
  status: MatriculaStatus;
  dataCriacao: string;
  dataAtualizacao: string;
}

export interface ListarMatriculasFiltro {
  status?: MatriculaStatus;
  alunoId?: number;
  turmaId?: number;
}

export async function listarMatriculas(filtro?: ListarMatriculasFiltro): Promise<MatriculaListItem[]> {
  const params = new URLSearchParams();

  if (filtro?.status) params.set("status", filtro.status);
  if (typeof filtro?.alunoId === "number") params.set("alunoId", String(filtro.alunoId));
  if (typeof filtro?.turmaId === "number") params.set("turmaId", String(filtro.turmaId));

  const query = params.toString();
  return apiRequest<MatriculaListItem[]>(`/api/matriculas${query ? `?${query}` : ""}`);
}

export async function cancelarMatriculaById(matriculaId: number): Promise<void> {
  await apiRequest<void>(`/api/matriculas/${matriculaId}/cancelar`, { method: "PATCH" });
}

export async function vincularTurmaMatricula(matriculaId: number, turmaId: number): Promise<void> {
  await apiRequest<void>(`/api/matriculas/${matriculaId}/vincular-turma`, {
    method: "PATCH",
    body: { turmaId },
  });
}
