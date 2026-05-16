import { apiRequest } from "@/lib/api/client";

/** 0=Domingo … 6=Sábado (padrão .NET DayOfWeek). */
export type HorarioFuncionamentoDto = {
  diaSemana: number;
  aberto: boolean;
  /** Formato "HH:mm". Nulo quando fechado. */
  horarioAbertura: string | null;
  /** Formato "HH:mm". Nulo quando fechado. */
  horarioFechamento: string | null;
};

export type AtualizarHorariosPayload = {
  dias: {
    diaSemana: number;
    aberto: boolean;
    horarioAbertura?: string | null;
    horarioFechamento?: string | null;
  }[];
};

export async function listarHorariosFuncionamento(): Promise<HorarioFuncionamentoDto[]> {
  return apiRequest<HorarioFuncionamentoDto[]>("/api/horarios-funcionamento");
}

/** Leitura para validar compromissos (perfis que agendam; nao exige VISUALIZAR_USUARIO). */
export async function listarHorariosFuncionamentoConsultaCompromissos(): Promise<HorarioFuncionamentoDto[]> {
  return apiRequest<HorarioFuncionamentoDto[]>("/api/horarios-funcionamento/consulta-compromissos");
}

/** Leitura da grade para validar horários ao criar/editar/ativar turmas. */
export async function listarHorariosFuncionamentoConsultaTurmas(): Promise<HorarioFuncionamentoDto[]> {
  return apiRequest<HorarioFuncionamentoDto[]>("/api/horarios-funcionamento/consulta-turmas");
}

export async function atualizarHorariosFuncionamento(
  payload: AtualizarHorariosPayload,
): Promise<HorarioFuncionamentoDto[]> {
  return apiRequest<HorarioFuncionamentoDto[]>("/api/horarios-funcionamento", {
    method: "PUT",
    body: payload,
  });
}
