import type { EventoCalendario } from "@/lib/api/calendario";
import { corEventoCalendario } from "@/lib/agenda/agenda-grade";

type Props = {
  eventoCalendario: EventoCalendario | null;
  escolaFechadaDiaInteiro: boolean;
  /** Coluna estreita (visão semanal) vs tela inteira (agenda global). */
  compacto?: boolean;
};

export function AgendaDiaAvisoOverlay({
  eventoCalendario,
  escolaFechadaDiaInteiro,
  compacto = false,
}: Props) {
  if (eventoCalendario) {
    const titulo = eventoCalendario.descricao ?? eventoCalendario.tipoEvento;
    return (
      <div
        className={`absolute inset-0 z-30 flex flex-col items-center justify-center border backdrop-blur-[1px] ${corEventoCalendario(eventoCalendario.tipoEvento)} ${compacto ? "bg-opacity-75" : "bg-opacity-90"} px-2 text-center`}
        role="status"
      >
        {!compacto && (
          <svg
            className="mb-2 opacity-80"
            width={compacto ? 32 : 48}
            height={compacto ? 32 : 48}
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            strokeWidth="1.5"
            aria-hidden
          >
            <rect x="3" y="4" width="18" height="18" rx="2" />
            <line x1="3" y1="10" x2="21" y2="10" />
          </svg>
        )}
        <p className={`font-bold uppercase tracking-wide ${compacto ? "text-[10px] leading-tight" : "text-sm"}`}>
          {titulo}
        </p>
        {!compacto && (
          <p className="mt-1 text-xs opacity-90">Sem aulas neste dia</p>
        )}
      </div>
    );
  }

  if (escolaFechadaDiaInteiro) {
    return (
      <div
        className="absolute inset-0 z-30 flex flex-col items-center justify-center border border-zinc-200 bg-zinc-100/85 px-2 text-center text-zinc-600 backdrop-blur-[1px]"
        role="status"
      >
        <p className={`font-semibold ${compacto ? "text-[10px] text-zinc-500" : "text-sm text-zinc-600"}`}>
          Escola fechada
        </p>
        {!compacto && (
          <p className="mt-1 text-xs text-zinc-500">Sem aulas neste dia da semana</p>
        )}
      </div>
    );
  }

  return null;
}
