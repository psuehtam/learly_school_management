import { apiRequest } from "@/lib/api/client";
import type {
  PreAlunoListItem,
  Contrato,
  ContratoTemplate,
  LivroInteresseOpcao,
  CriarPreAlunoPayload,
} from "@/types/comercial";
import type { Aluno } from "@/types/aluno";

export async function listarLivrosInteressePreAluno(): Promise<LivroInteresseOpcao[]> {
  return apiRequest<LivroInteresseOpcao[]>("/api/pre-alunos/livros-interesse");
}

export async function listarPreAlunos(filtros?: Record<string, string>): Promise<PreAlunoListItem[]> {
  const params = filtros ? `?${new URLSearchParams(filtros)}` : "";
  return apiRequest<PreAlunoListItem[]>(`/api/pre-alunos${params}`);
}

export async function buscarPreAluno(id: number): Promise<unknown> {
  return apiRequest<unknown>(`/api/pre-alunos/${id}`);
}

export async function criarPreAluno(dados: CriarPreAlunoPayload): Promise<{ id: number }> {
  return apiRequest<{ id: number }>("/api/pre-alunos", { method: "POST", body: dados });
}

export async function editarPreAluno(id: number, dados: Partial<unknown>): Promise<unknown> {
  return apiRequest<unknown>(`/api/pre-alunos/${id}`, { method: "PUT", body: dados });
}

export async function cancelarPreAluno(id: number): Promise<void> {
  await apiRequest<void>(`/api/pre-alunos/${id}/cancelar`, { method: "PATCH" });
}

export async function submeterPreAlunoParaAprovacao(id: number): Promise<void> {
  await apiRequest<void>(`/api/pre-alunos/${id}/submeter-aprovacao`, { method: "PATCH" });
}

export async function aprovarMatricula(preAlunoId: number): Promise<void> {
  await apiRequest<void>(`/api/pre-alunos/${preAlunoId}/aprovar`, { method: "PATCH" });
}

export async function reprovarMatricula(preAlunoId: number, motivo: string): Promise<void> {
  await apiRequest<void>(`/api/pre-alunos/${preAlunoId}/reprovar`, {
    method: "PATCH",
    body: { motivo },
  });
}

export async function finalizarMatricula(preAlunoId: number): Promise<Aluno> {
  return apiRequest<Aluno>(`/api/pre-alunos/${preAlunoId}/finalizar`, { method: "POST" });
}

export async function listarContratos(preAlunoId: number): Promise<Contrato[]> {
  return apiRequest<Contrato[]>(`/api/pre-alunos/${preAlunoId}/contratos`);
}

export async function gerarContrato(preAlunoId: number): Promise<Contrato> {
  return apiRequest<Contrato>(`/api/pre-alunos/${preAlunoId}/contratos`, { method: "POST" });
}

export async function buscarTemplateAtivo(): Promise<ContratoTemplate> {
  return apiRequest<ContratoTemplate>("/api/contratos/template-ativo");
}
