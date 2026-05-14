import { HorariosFuncionamentoPanel } from "@/components/escola/horarios-funcionamento-panel";

export default function HorariosFuncionamentoPage() {
  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-xl font-semibold text-zinc-900">Horário de funcionamento</h1>
        <p className="text-sm text-zinc-500 mt-0.5">
          Defina em quais dias da semana a escola abre e o intervalo de atendimento. Quem pode gerenciar usuários da
          escola ou configurações do sistema pode editar esta grade.
        </p>
      </div>
      <HorariosFuncionamentoPanel />
    </div>
  );
}
