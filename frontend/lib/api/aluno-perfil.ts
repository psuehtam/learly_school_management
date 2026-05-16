import { apiRequest, ApiError } from "@/lib/api/client";

export type TipoOcorrenciaAluno = "Acadêmica" | "Administrativa";

export interface AlunoOcorrencia {
  id: number;
  data: string;
  hora: string;
  tipo: TipoOcorrenciaAluno;
  descricao: string;
  resolucao: string;
  aulaVinculada: string;
  autor: string;
}

export interface AlunoDocumento {
  id: number;
  nome: string;
  tipo: string;
  dataEnvio: string;
  tamanho: string;
  autor: string;
}

export interface AlunoFalta {
  id: number;
  data: string;
  book: string;
  aula: string;
  justificada: boolean;
  motivo?: string;
  autorJustificativa?: string;
  dataJustificativa?: string;
}

export interface SalvarOcorrenciaPayload {
  data: string;
  hora: string;
  tipo: TipoOcorrenciaAluno;
  descricao: string;
  resolucao?: string;
  aulaVinculada?: string;
}

function normalizarOcorrencia(raw: AlunoOcorrencia & Record<string, unknown>): AlunoOcorrencia {
  const r = raw as AlunoOcorrencia & {
    Tipo?: string;
    Descricao?: string;
    Resolucao?: string;
    AulaVinculada?: string;
    Autor?: string;
    Data?: string;
    Hora?: string;
  };
  const tipoRaw = (raw.tipo ?? r.Tipo ?? "Acadêmica") as string;
  const tipo: TipoOcorrenciaAluno =
    tipoRaw.toLowerCase().includes("admin") ? "Administrativa" : "Acadêmica";

  return {
    id: raw.id,
    data: raw.data ?? r.Data ?? "",
    hora: raw.hora ?? r.Hora ?? "",
    tipo,
    descricao: (raw.descricao ?? r.Descricao ?? "").trim(),
    resolucao: (raw.resolucao ?? r.Resolucao ?? "").trim(),
    aulaVinculada: (raw.aulaVinculada ?? r.AulaVinculada ?? "").trim(),
    autor: (raw.autor ?? r.Autor ?? "").trim(),
  };
}

function normalizarDocumento(raw: AlunoDocumento & Record<string, unknown>): AlunoDocumento {
  const r = raw as AlunoDocumento & {
    Nome?: string;
    Tipo?: string;
    DataEnvio?: string;
    Tamanho?: string;
    Autor?: string;
  };
  return {
    id: raw.id,
    nome: (raw.nome ?? r.Nome ?? "").trim(),
    tipo: (raw.tipo ?? r.Tipo ?? "").trim(),
    dataEnvio: raw.dataEnvio ?? r.DataEnvio ?? "",
    tamanho: (raw.tamanho ?? r.Tamanho ?? "").trim(),
    autor: (raw.autor ?? r.Autor ?? "").trim(),
  };
}

function normalizarFalta(raw: AlunoFalta & Record<string, unknown>): AlunoFalta {
  const r = raw as AlunoFalta & {
    Book?: string;
    Aula?: string;
    Justificada?: boolean;
    Motivo?: string;
    AutorJustificativa?: string;
    DataJustificativa?: string;
    Data?: string;
  };
  return {
    id: raw.id,
    data: raw.data ?? r.Data ?? "",
    book: (raw.book ?? r.Book ?? "").trim(),
    aula: (raw.aula ?? r.Aula ?? "").trim(),
    justificada: raw.justificada ?? r.Justificada ?? false,
    motivo: raw.motivo ?? r.Motivo,
    autorJustificativa: raw.autorJustificativa ?? r.AutorJustificativa,
    dataJustificativa: raw.dataJustificativa ?? r.DataJustificativa,
  };
}

export async function listarOcorrenciasAluno(alunoId: number): Promise<AlunoOcorrencia[]> {
  const data = await apiRequest<AlunoOcorrencia[]>(`/api/alunos/${alunoId}/ocorrencias`);
  return data.map((item) => normalizarOcorrencia(item as AlunoOcorrencia & Record<string, unknown>));
}

export async function criarOcorrenciaAluno(
  alunoId: number,
  payload: SalvarOcorrenciaPayload,
): Promise<AlunoOcorrencia | null> {
  const data = await apiRequest<AlunoOcorrencia | null>(`/api/alunos/${alunoId}/ocorrencias`, {
    method: "POST",
    body: payload,
  });
  return data ? normalizarOcorrencia(data as AlunoOcorrencia & Record<string, unknown>) : null;
}

export async function atualizarOcorrenciaAluno(
  alunoId: number,
  ocorrenciaId: number,
  payload: SalvarOcorrenciaPayload,
): Promise<AlunoOcorrencia | null> {
  const data = await apiRequest<AlunoOcorrencia | null>(
    `/api/alunos/${alunoId}/ocorrencias/${ocorrenciaId}`,
    { method: "PUT", body: payload },
  );
  return data ? normalizarOcorrencia(data as AlunoOcorrencia & Record<string, unknown>) : null;
}

export async function listarDocumentosAluno(alunoId: number): Promise<AlunoDocumento[]> {
  const data = await apiRequest<AlunoDocumento[]>(`/api/alunos/${alunoId}/documentos`);
  return data.map((item) => normalizarDocumento(item as AlunoDocumento & Record<string, unknown>));
}

export async function listarFaltasAluno(alunoId: number): Promise<AlunoFalta[]> {
  const data = await apiRequest<AlunoFalta[]>(`/api/alunos/${alunoId}/faltas`);
  return data.map((item) => normalizarFalta(item as AlunoFalta & Record<string, unknown>));
}

export async function justificarFaltaAluno(
  alunoId: number,
  presencaId: number,
  motivo: string,
): Promise<void> {
  await apiRequest<void>(`/api/alunos/${alunoId}/faltas/${presencaId}/justificar`, {
    method: "PATCH",
    body: { motivo },
  });
}

export function isRecursoEmDesenvolvimento(error: unknown): boolean {
  return error instanceof ApiError && error.status === 501;
}
