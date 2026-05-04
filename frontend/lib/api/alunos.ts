import { apiRequest } from "@/lib/api/client";
import type { Aluno, Filiacao } from "@/types/aluno";

export interface CriarAlunoPayload {
  eProprioResponsavel: boolean;
  nome: string;
  sobrenome: string;
  sexo: "Masculino" | "Feminino" | "Outro";
  dataNascimento: string;
  dataIngresso: string;
  cpf?: string;
  cep: string;
  tipoLogradouro: "Rua" | "Avenida" | "Travessa" | "Alameda" | "Estrada" | "Rodovia" | "Outro";
  logradouro: string;
  numero: string;
  complemento?: string;
  bairro: string;
  municipio: string;
  responsavelNome?: string;
  responsavelSobrenome?: string;
  responsavelCpf?: string;
  responsavelSexo?: "Masculino" | "Feminino" | "Outro";
  responsavelCep?: string;
  responsavelTipoLogradouro?: "Rua" | "Avenida" | "Travessa" | "Alameda" | "Estrada" | "Rodovia" | "Outro";
  responsavelLogradouro?: string;
  responsavelNumero?: string;
  responsavelComplemento?: string;
  responsavelBairro?: string;
  responsavelMunicipio?: string;
}

export interface CriarAlunoComMatriculaResponse {
  alunoId: number;
  matriculaId: number;
}

export async function listarAlunos(filtros?: Record<string, string>): Promise<Aluno[]> {
  const params = filtros ? `?${new URLSearchParams(filtros)}` : "";
  return apiRequest<Aluno[]>(`/api/alunos${params}`);
}

export async function buscarAluno(id: number): Promise<Aluno> {
  return apiRequest<Aluno>(`/api/alunos/${id}`);
}

export async function criarAluno(dados: Partial<Aluno>): Promise<Aluno> {
  return apiRequest<Aluno>("/api/alunos", { method: "POST", body: dados });
}

export async function criarAlunoComMatricula(dados: CriarAlunoPayload): Promise<CriarAlunoComMatriculaResponse> {
  return apiRequest<CriarAlunoComMatriculaResponse>("/api/alunos", {
    method: "POST",
    body: dados,
  });
}

export async function editarAluno(id: number, dados: Partial<Aluno>): Promise<Aluno> {
  return apiRequest<Aluno>(`/api/alunos/${id}`, { method: "PUT", body: dados });
}

export async function inativarAluno(id: number): Promise<void> {
  await apiRequest<void>(`/api/alunos/${id}/inativar`, { method: "PATCH" });
}

export async function trancarAluno(id: number): Promise<void> {
  await apiRequest<void>(`/api/alunos/${id}/trancar`, { method: "PATCH" });
}

export async function listarFiliacoes(alunoId: number): Promise<Filiacao[]> {
  return apiRequest<Filiacao[]>(`/api/alunos/${alunoId}/filiacoes`);
}

export async function criarFiliacao(alunoId: number, dados: Partial<Filiacao>): Promise<Filiacao> {
  return apiRequest<Filiacao>(`/api/alunos/${alunoId}/filiacoes`, { method: "POST", body: dados });
}
