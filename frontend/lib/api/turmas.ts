import { apiRequest } from "@/lib/api/client";
import type { Turma, Matricula, ProgressoCapitulo, CriarTurmaPayload, AtualizarTurmaPayload } from "@/types/turma";
import type { Aluno } from "@/types/aluno";

export async function listarTurmas(filtros?: Record<string, string>): Promise<Turma[]> {
  const params = filtros ? `?${new URLSearchParams(filtros)}` : "";
  return apiRequest<Turma[]>(`/api/turmas${params}`);
}

export async function buscarTurma(id: number): Promise<Turma> {
  return apiRequest<Turma>(`/api/turmas/${id}`);
}

export async function criarTurma(dados: CriarTurmaPayload): Promise<Turma> {
  return apiRequest<Turma>("/api/turmas", { method: "POST", body: dados });
}

export async function editarTurma(id: number, dados: AtualizarTurmaPayload): Promise<Turma> {
  return apiRequest<Turma>(`/api/turmas/${id}`, { method: "PUT", body: dados });
}

export type AtivarTurmaPayload = {
  dataInicio: string;
  diasSemana?: number[];
  horarioInicio?: string;
  horarioFim?: string;
  sala?: string;
};

export async function ativarTurma(id: number, dados: AtivarTurmaPayload): Promise<Turma> {
  return apiRequest<Turma>(`/api/turmas/${id}/ativar`, { method: "POST", body: dados });
}

export async function concluirTurma(id: number): Promise<Turma> {
  return apiRequest<Turma>(`/api/turmas/${id}/concluir`, { method: "POST" });
}

export async function inativarTurma(id: number): Promise<Turma> {
  return apiRequest<Turma>(`/api/turmas/${id}/inativar`, { method: "POST" });
}

export async function listarAlunosDaTurma(turmaId: number): Promise<Aluno[]> {
  return apiRequest<Aluno[]>(`/api/turmas/${turmaId}/alunos`);
}

export async function matricularAluno(turmaId: number, alunoId: number): Promise<Matricula> {
  return apiRequest<Matricula>(`/api/turmas/${turmaId}/matriculas`, {
    method: "POST",
    body: { alunoId },
  });
}

export async function cancelarMatricula(matriculaId: number): Promise<void> {
  await apiRequest<void>(`/api/matriculas/${matriculaId}/cancelar`, { method: "PATCH" });
}

export async function listarProgressoCapitulos(turmaId: number): Promise<ProgressoCapitulo[]> {
  return apiRequest<ProgressoCapitulo[]>(`/api/turmas/${turmaId}/progresso`);
}

export async function marcarCapituloConcluido(turmaId: number, capituloId: number): Promise<void> {
  await apiRequest<void>(`/api/turmas/${turmaId}/progresso/${capituloId}`, { method: "PATCH" });
}

export type AvaliacaoTurma = {
  id: number;
  turmaId: number;
  alunoId: number;
  tipo: string;
  nota: number;
};

export async function listarAvaliacoesTurma(turmaId: number): Promise<AvaliacaoTurma[]> {
  return apiRequest<AvaliacaoTurma[]>(`/api/turmas/${turmaId}/avaliacoes`);
}

export async function salvarAvaliacoesTurma(
  turmaId: number,
  avaliacoes: { alunoId: number; tipo: string; nota: number }[],
): Promise<void> {
  await apiRequest<void>(`/api/turmas/${turmaId}/avaliacoes`, {
    method: "POST",
    body: { avaliacoes },
  });
}
