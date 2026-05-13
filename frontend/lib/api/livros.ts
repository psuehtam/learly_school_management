import { apiRequest } from "@/lib/api/client";

export type LivroCapituloEscolaDto = {
  id: number;
  nome: string;
  qtdAulasPrevistas: number;
  status: string;
};

/** Catálogo de livros/níveis da escola (`livros` + totais de `capitulos` / `qtd_aulas_previstas`). */
export type LivroEscolaDto = {
  id: number;
  nome: string;
  status: "Ativo" | "Inativo";
  quantidadeCapitulos: number;
  totalAulasPrevistas: number;
  /** Preenchido no GET por id; ausente ou `null` na listagem. */
  capitulos?: LivroCapituloEscolaDto[] | null;
};

export type AtualizarLivroEscolaPayload = {
  nome?: string;
  status?: "Ativo" | "Inativo";
  capitulosAulas?: { capituloId: number; qtdAulasPrevistas: number }[];
  /** Novos capítulos no final do livro (nome opcional; se vazio, o backend usa "Capítulo N"). */
  capitulosNovos?: { nome?: string; qtdAulasPrevistas: number }[];
};

export async function listarLivrosEscola(): Promise<LivroEscolaDto[]> {
  return apiRequest<LivroEscolaDto[]>("/api/livros");
}

export async function obterLivroEscola(id: number): Promise<LivroEscolaDto> {
  return apiRequest<LivroEscolaDto>(`/api/livros/${id}`);
}

export async function criarLivroEscola(payload: {
  nome: string;
  quantidadeCapitulos: number;
  /** Um valor por capítulo (ordem: Capítulo 1 … N), alinhado a `qtd_aulas_previstas` no banco. */
  aulasPrevistasPorCapitulo: number[];
}): Promise<LivroEscolaDto> {
  return apiRequest<LivroEscolaDto>("/api/livros", {
    method: "POST",
    body: payload,
  });
}

export async function atualizarLivroEscola(
  id: number,
  payload: AtualizarLivroEscolaPayload,
): Promise<LivroEscolaDto> {
  return apiRequest<LivroEscolaDto>(`/api/livros/${id}`, {
    method: "PATCH",
    body: payload,
  });
}
