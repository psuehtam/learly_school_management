import { apiRequest } from "@/lib/api/client";

export type DashboardGeral = {
  resumo: {
    alunosAtivos: number;
    turmasAtivas: number;
    professores: number;
    parcelasEmAberto: number;
  };
  aulasHoje: {
    aulaId: number;
    turmaNome: string;
    professorNome: string;
    horario: string;
    turno: "morning" | "afternoon" | "evening";
    totalAlunos: number;
  }[];
  parcelasVencidas: {
    parcelaId: number;
    alunoNome: string;
    turmaNome: string;
    dataVencimento: string;
    valor: number;
  }[];
  atividadeRecente: {
    acao: string;
    detalhe: string;
    ocorridoEm: string;
    tipo: string;
  }[];
};

function normalizar(raw: DashboardGeral & Record<string, unknown>): DashboardGeral {
  const r = raw as DashboardGeral & {
    Resumo?: DashboardGeral["resumo"];
    AulasHoje?: DashboardGeral["aulasHoje"];
    ParcelasVencidas?: DashboardGeral["parcelasVencidas"];
    AtividadeRecente?: DashboardGeral["atividadeRecente"];
  };
  return {
    resumo: raw.resumo ?? r.Resumo ?? { alunosAtivos: 0, turmasAtivas: 0, professores: 0, parcelasEmAberto: 0 },
    aulasHoje: raw.aulasHoje ?? r.AulasHoje ?? [],
    parcelasVencidas: raw.parcelasVencidas ?? r.ParcelasVencidas ?? [],
    atividadeRecente: raw.atividadeRecente ?? r.AtividadeRecente ?? [],
  };
}

export async function buscarDashboardGeral(data?: string): Promise<DashboardGeral> {
  const params = data ? `?data=${encodeURIComponent(data)}` : "";
  const dataRaw = await apiRequest<DashboardGeral>(`/api/dashboard${params}`);
  return normalizar(dataRaw as DashboardGeral & Record<string, unknown>);
}
