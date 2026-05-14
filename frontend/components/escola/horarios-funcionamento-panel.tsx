"use client";

import { useCallback, useEffect, useState } from "react";
import { getCurrentUser } from "@/lib/api/auth";
import { getApiErrorMessage } from "@/lib/api/client";
import {
  listarHorariosFuncionamento,
  atualizarHorariosFuncionamento,
  type HorarioFuncionamentoDto,
} from "@/lib/api/configuracoes";
import { hasAnyPermission } from "@/lib/permissions";
import type { User } from "@/lib/api/types";
import { Button } from "@/components/ui/button";
import { Card, CardHeader, CardTitle } from "@/components/ui/card";

const DIAS_SEMANA = [
  { valor: 0, label: "Domingo" },
  { valor: 1, label: "Segunda-feira" },
  { valor: 2, label: "Terça-feira" },
  { valor: 3, label: "Quarta-feira" },
  { valor: 4, label: "Quinta-feira" },
  { valor: 5, label: "Sexta-feira" },
  { valor: 6, label: "Sábado" },
];

type DiaForm = {
  diaSemana: number;
  aberto: boolean;
  horarioAbertura: string;
  horarioFechamento: string;
};

function horarioParaForm(h: HorarioFuncionamentoDto): DiaForm {
  return {
    diaSemana: h.diaSemana,
    aberto: h.aberto,
    horarioAbertura: h.horarioAbertura ?? "",
    horarioFechamento: h.horarioFechamento ?? "",
  };
}

function diasPadrao(): DiaForm[] {
  return DIAS_SEMANA.map((d) => ({
    diaSemana: d.valor,
    aberto: d.valor >= 1 && d.valor <= 5,
    horarioAbertura: d.valor >= 1 && d.valor <= 5 ? "08:00" : d.valor === 6 ? "08:00" : "",
    horarioFechamento: d.valor >= 1 && d.valor <= 5 ? "18:00" : d.valor === 6 ? "12:00" : "",
  }));
}

function mesclaDiasComApi(apiDias: HorarioFuncionamentoDto[]): DiaForm[] {
  const mapa = new Map(apiDias.map((d) => [d.diaSemana, d]));
  return DIAS_SEMANA.map((d) => {
    const api = mapa.get(d.valor);
    if (api) return horarioParaForm(api);
    return { diaSemana: d.valor, aberto: false, horarioAbertura: "", horarioFechamento: "" };
  });
}

const INPUT =
  "h-9 w-full rounded-lg border border-zinc-300 bg-white px-3 text-sm text-zinc-900 shadow-sm outline-none focus:border-[#1F2A35] focus:ring-2 focus:ring-[#1F2A35]/20 disabled:cursor-not-allowed disabled:bg-zinc-100 disabled:text-zinc-400";

export function HorariosFuncionamentoPanel() {
  const [user, setUser] = useState<User | null>(null);
  const [dias, setDias] = useState<DiaForm[]>(diasPadrao());
  const [loading, setLoading] = useState(true);
  const [salvando, setSalvando] = useState(false);
  const [erro, setErro] = useState<string | null>(null);
  const [sucesso, setSucesso] = useState(false);

  const podeEditar = user
    ? hasAnyPermission(user, ["VISUALIZAR_USUARIO", "GERENCIAR_CONFIGURACOES_SISTEMA"])
    : false;

  useEffect(() => {
    void getCurrentUser().then(setUser).catch(() => setUser(null));
  }, []);

  const carregar = useCallback(async () => {
    setLoading(true);
    setErro(null);
    try {
      const data = await listarHorariosFuncionamento();
      setDias(mesclaDiasComApi(data));
    } catch {
      setDias(diasPadrao());
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void carregar();
  }, [carregar]);

  function setDia(index: number, changes: Partial<DiaForm>) {
    setDias((prev) => {
      const next = [...prev];
      next[index] = { ...next[index]!, ...changes };
      return next;
    });
    setSucesso(false);
  }

  async function salvar() {
    setErro(null);
    setSucesso(false);

    for (const d of dias) {
      if (d.aberto) {
        if (!d.horarioAbertura || !d.horarioFechamento) {
          setErro(`Informe os horários de abertura e fechamento para ${DIAS_SEMANA[d.diaSemana]?.label}.`);
          return;
        }
        if (d.horarioAbertura >= d.horarioFechamento) {
          setErro(
            `O horário de fechamento deve ser maior que o de abertura em ${DIAS_SEMANA[d.diaSemana]?.label}.`,
          );
          return;
        }
      }
    }

    setSalvando(true);
    try {
      const data = await atualizarHorariosFuncionamento({
        dias: dias.map((d) => ({
          diaSemana: d.diaSemana,
          aberto: d.aberto,
          horarioAbertura: d.aberto ? d.horarioAbertura || null : null,
          horarioFechamento: d.aberto ? d.horarioFechamento || null : null,
        })),
      });
      setDias(mesclaDiasComApi(data));
      setSucesso(true);
    } catch (e) {
      setErro(getApiErrorMessage(e, "Não foi possível salvar os horários."));
    } finally {
      setSalvando(false);
    }
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>Horários de funcionamento</CardTitle>
      </CardHeader>

      <div className="px-6 pb-6">
        <p className="mb-5 text-sm text-zinc-500">
          Defina para cada dia da semana se a escola abre e em qual horário. Esses horários são usados para validar
          compromissos e outras operações que dependem do funcionamento da escola.
        </p>

        {loading ? (
          <div className="flex items-center gap-2 py-8 text-sm text-zinc-500">
            <svg className="h-4 w-4 animate-spin" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
              <circle cx="12" cy="12" r="10" strokeOpacity=".25" />
              <path d="M12 2a10 10 0 0 1 10 10" />
            </svg>
            Carregando…
          </div>
        ) : (
          <div className="overflow-x-auto rounded-xl border border-zinc-200">
            <table className="w-full min-w-[560px] border-collapse text-sm">
              <thead>
                <tr className="border-b border-zinc-200 bg-zinc-50">
                  <th className="px-4 py-3 text-left font-medium text-zinc-600">Dia</th>
                  <th className="px-4 py-3 text-center font-medium text-zinc-600">Abre?</th>
                  <th className="px-4 py-3 text-left font-medium text-zinc-600">Abertura</th>
                  <th className="px-4 py-3 text-left font-medium text-zinc-600">Fechamento</th>
                </tr>
              </thead>
              <tbody>
                {dias.map((dia, i) => {
                  const info = DIAS_SEMANA[dia.diaSemana]!;
                  const ehFimDeSemana = dia.diaSemana === 0 || dia.diaSemana === 6;
                  return (
                    <tr
                      key={dia.diaSemana}
                      className={`border-b border-zinc-100 last:border-0 ${
                        !dia.aberto ? "bg-zinc-50/60" : ""
                      } ${ehFimDeSemana ? "bg-amber-50/30" : ""}`}
                    >
                      <td className="px-4 py-3">
                        <span className={`font-medium ${ehFimDeSemana ? "text-amber-700" : "text-zinc-900"}`}>
                          {info.label}
                        </span>
                      </td>
                      <td className="px-4 py-3 text-center">
                        <label className="relative inline-flex cursor-pointer items-center">
                          <input
                            type="checkbox"
                            className="peer sr-only"
                            checked={dia.aberto}
                            disabled={!podeEditar}
                            onChange={(e) => {
                              const aberto = e.target.checked;
                              setDia(i, {
                                aberto,
                                horarioAbertura: aberto && !dia.horarioAbertura ? "08:00" : dia.horarioAbertura,
                                horarioFechamento:
                                  aberto && !dia.horarioFechamento
                                    ? dia.diaSemana === 6
                                      ? "12:00"
                                      : "18:00"
                                    : dia.horarioFechamento,
                              });
                            }}
                          />
                          <span className="h-6 w-11 rounded-full bg-zinc-300 transition-colors peer-checked:bg-[#1F2A35] peer-disabled:cursor-not-allowed peer-disabled:opacity-50" />
                          <span className="absolute left-0.5 top-0.5 h-5 w-5 rounded-full bg-white shadow transition-transform peer-checked:translate-x-5" />
                        </label>
                      </td>
                      <td className="px-4 py-3">
                        <input
                          type="time"
                          className={INPUT}
                          value={dia.horarioAbertura}
                          disabled={!dia.aberto || !podeEditar}
                          onChange={(e) => setDia(i, { horarioAbertura: e.target.value })}
                        />
                      </td>
                      <td className="px-4 py-3">
                        <input
                          type="time"
                          className={INPUT}
                          value={dia.horarioFechamento}
                          disabled={!dia.aberto || !podeEditar}
                          onChange={(e) => setDia(i, { horarioFechamento: e.target.value })}
                        />
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}

        {erro && (
          <div className="mt-4 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800">
            {erro}
          </div>
        )}

        {sucesso && (
          <div className="mt-4 rounded-lg border border-green-200 bg-green-50 px-4 py-3 text-sm text-green-800">
            Horários salvos com sucesso.
          </div>
        )}

        {podeEditar && !loading && (
          <div className="mt-5 flex justify-end">
            <Button onClick={() => void salvar()} isLoading={salvando} disabled={salvando}>
              Salvar horários
            </Button>
          </div>
        )}

        {!podeEditar && !loading && (
          <p className="mt-4 text-xs text-zinc-400">
            Você não tem permissão para alterar os horários de funcionamento da escola.
          </p>
        )}
      </div>
    </Card>
  );
}
