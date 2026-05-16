type Faixa = { topPx: number; heightPx: number };

type Props = {
  faixas: Faixa[];
  /** Colunas estreitas da visão semanal (minha agenda). */
  compacto?: boolean;
};

/**
 * Zonas fora do horário de funcionamento — visual discreto (não compete com aulas/compromissos).
 */
export function AgendaForaExpedienteFaixas({ faixas, compacto = false }: Props) {
  if (faixas.length === 0) return null;

  const minAlturaParaRotulo = compacto ? 72 : 48;

  return (
    <>
      {faixas.map((faixa, fi) => (
        <div
          key={fi}
          className="pointer-events-none absolute inset-x-0 z-[1] border-y border-zinc-200/80 bg-zinc-100/70"
          style={{
            top: faixa.topPx,
            height: faixa.heightPx,
            minHeight: 4,
            backgroundImage:
              "repeating-linear-gradient(-45deg, transparent, transparent 5px, rgba(113,113,122,0.07) 5px, rgba(113,113,122,0.07) 10px)",
          }}
          title="Fora do horário de funcionamento da escola"
          aria-hidden
        >
          {faixa.heightPx >= minAlturaParaRotulo && (
            <span
              className={`absolute left-1.5 font-medium uppercase tracking-wider text-zinc-400 ${
                compacto ? "top-1 text-[8px] leading-none" : "top-1.5 text-[10px]"
              }`}
            >
              {compacto ? "Fechado" : "Fora do expediente"}
            </span>
          )}
        </div>
      ))}
    </>
  );
}
