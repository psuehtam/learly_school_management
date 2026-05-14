"use client";

import type { CSSProperties } from "react";
import type { OcupacaoAgendaItem } from "@/lib/agenda";
import {
  ocupacaoAgendaCardClassNames,
  ocupacaoAgendaTipoBadgeClassNames,
  ocupacaoAgendaTipoLegivel,
} from "@/lib/agenda";

export interface AgendaOcupacaoCardProps {
  item: OcupacaoAgendaItem;
  /** Posição vertical na grade (px), já calculada a partir do horário. */
  topPx: number;
  /** Altura exibida na grade (px); use `max(duração real, mínimo)` para blocos muito curtos. */
  alturaExibicaoPx: number;
  /** Quando há sobreposição na coluna, divide a largura (índice 0..total-1). */
  faixaHorizontal?: { indice: number; total: number };
  /** Quando false, o card fica esmaecido (ex.: filtro de busca). */
  temDestaque: boolean;
  /** Classe Tailwind da faixa lateral (ex.: cor da coluna do usuário). */
  corBarraLateral: string;
  /** Ex.: `opacity-100 hover:shadow-md z-10` vs esmaecido. */
  classNameDestaque?: string;
  onClick?: () => void;
}

/**
 * Card da grade de agenda: aula, reposição ou compromisso, com tipo sempre visível (badge).
 */
export function AgendaOcupacaoCard({
  item,
  topPx,
  alturaExibicaoPx,
  faixaHorizontal = { indice: 0, total: 1 },
  temDestaque,
  corBarraLateral,
  classNameDestaque = temDestaque
    ? "opacity-100 hover:shadow-md hover:z-[40]"
    : "opacity-30 grayscale hover:opacity-50 z-[1]",
  onClick,
}: AgendaOcupacaoCardProps) {
  const tipoPrincipal = ocupacaoAgendaTipoLegivel(item.tipo);
  const dicaNativa = [tipoPrincipal, item.titulo, `${item.inicio} – ${item.fim}`, item.subtitulo]
    .filter(Boolean)
    .join(" · ");

  const { indice, total } = faixaHorizontal;
  const pad = 4;
  const gap = 2;
  const zBase = 6 + indice;
  const posicaoStyle: CSSProperties =
    total <= 1
      ? {
          top: topPx,
          height: alturaExibicaoPx,
          left: pad,
          right: pad,
          zIndex: zBase,
        }
      : {
          top: topPx,
          height: alturaExibicaoPx,
          right: "auto",
          zIndex: zBase,
          width: `calc((100% - ${2 * pad + (total - 1) * gap}px) / ${total})`,
          left: `calc(${pad}px + ${indice} * ((100% - ${2 * pad + (total - 1) * gap}px) / ${total} + ${gap}px))`,
        };

  return (
    <div
      role={onClick ? "button" : undefined}
      tabIndex={onClick ? 0 : undefined}
      aria-label={`${tipoPrincipal}: ${item.titulo}, ${item.inicio} até ${item.fim}`}
      title={dicaNativa}
      onClick={onClick}
      onKeyDown={
        onClick
          ? (e) => {
              if (e.key === "Enter" || e.key === " ") {
                e.preventDefault();
                onClick();
              }
            }
          : undefined
      }
      style={posicaoStyle}
      className={`absolute rounded-xl px-2 py-1.5 border overflow-hidden transition-all duration-200 ${onClick ? "cursor-pointer" : ""} ${classNameDestaque} ${ocupacaoAgendaCardClassNames(item.tipo)}`}
    >
      <div className={`absolute left-0 top-0 bottom-0 w-1 rounded-l-xl ${corBarraLateral}`} />

      <div className="flex flex-col h-full min-h-0 ml-2 gap-1 justify-start text-zinc-900">
        <div className="flex flex-wrap items-center gap-1 shrink-0">
          <span
            className={`inline-flex items-center rounded-md px-1.5 py-0.5 text-[9px] font-bold tracking-tight leading-none ${ocupacaoAgendaTipoBadgeClassNames(item.tipo)}`}
          >
            {tipoPrincipal}
          </span>
          {item.tipo === "COMPROMISSO" && item.categoriaCompromisso ? (
            <span className="text-[9px] font-semibold text-violet-900/85 leading-none truncate max-w-[min(100%,8rem)]">
              {item.categoriaCompromisso}
            </span>
          ) : null}
        </div>

        <h4 className="text-xs font-bold leading-snug text-zinc-900 line-clamp-2 shrink-0">{item.titulo}</h4>

        <p className="text-[10px] font-semibold text-zinc-600 tabular-nums shrink-0">
          {item.inicio} – {item.fim}
        </p>

        {item.contextoAulaExtra ? (
          <p className="text-[10px] leading-snug text-zinc-700 font-medium line-clamp-2 shrink-0">
            {item.contextoAulaExtra}
          </p>
        ) : null}

        <p className="text-[10px] leading-snug text-zinc-500 line-clamp-2 mt-auto min-h-0">{item.subtitulo}</p>
      </div>
    </div>
  );
}
