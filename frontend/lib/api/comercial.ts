import { apiRequest } from "@/lib/api/client";
import type {
  PreAlunoListItem,
  ContratoGerado,
  ContratoTemplate,
  ContratoVariavel,
  ContratoGeradoData,
  CriarContratoTemplatePayload,
  EditarContratoTemplatePayload,
  GerarContratoPayload,
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

// ──────────────── Templates de Contrato ────────────────

export async function listarContratoTemplates(): Promise<ContratoTemplate[]> {
  return apiRequest<ContratoTemplate[]>("/api/contratos/templates");
}

export async function buscarContratoTemplateAtivo(): Promise<ContratoTemplate> {
  return apiRequest<ContratoTemplate>("/api/contratos/templates/ativo");
}

export async function buscarContratoTemplate(id: number): Promise<ContratoTemplate> {
  return apiRequest<ContratoTemplate>(`/api/contratos/templates/${id}`);
}

export async function criarContratoTemplate(dados: CriarContratoTemplatePayload): Promise<void> {
  await apiRequest<void>("/api/contratos/templates", { method: "POST", body: dados });
}

export async function editarContratoTemplate(id: number, dados: EditarContratoTemplatePayload): Promise<void> {
  await apiRequest<void>(`/api/contratos/templates/${id}`, { method: "PUT", body: dados });
}

export async function ativarContratoTemplate(id: number): Promise<void> {
  await apiRequest<void>(`/api/contratos/templates/${id}/ativar`, { method: "PATCH" });
}

export async function inativarContratoTemplate(id: number): Promise<void> {
  await apiRequest<void>(`/api/contratos/templates/${id}/inativar`, { method: "PATCH" });
}

export async function listarVariaveisContrato(): Promise<ContratoVariavel[]> {
  return apiRequest<ContratoVariavel[]>("/api/contratos/templates/variaveis");
}

// ──────────────── Contratos Gerados ────────────────

export async function listarContratosGerados(): Promise<ContratoGerado[]> {
  return apiRequest<ContratoGerado[]>("/api/contratos/gerados");
}

export async function listarContratosGeradosPorPreAluno(preAlunoId: number): Promise<ContratoGerado[]> {
  return apiRequest<ContratoGerado[]>(`/api/contratos/gerados/pre-aluno/${preAlunoId}`);
}

export async function gerarContrato(dados: GerarContratoPayload): Promise<ContratoGeradoData> {
  return apiRequest<ContratoGeradoData>("/api/contratos/gerar", { method: "POST", body: dados });
}
