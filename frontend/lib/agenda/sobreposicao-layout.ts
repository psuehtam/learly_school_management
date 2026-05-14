import type { OcupacaoAgendaItem } from "./ocupacao-agenda";

/**
 * Item já com posição na grade (px). Usado para detectar colisão **visual**
 * (ex.: altura mínima faz o card invadir o slot seguinte mesmo sem conflito de horário no relógio).
 */
export type ItemAgendaPosicionado = {
  item: OcupacaoAgendaItem;
  topPx: number;
  alturaExibicaoPx: number;
};

/** Intervalo vertical na grade [top, bottom) em px. */
function intervaloVisualPx(p: ItemAgendaPosicionado): { top: number; bottom: number } {
  return {
    top: p.topPx,
    bottom: p.topPx + p.alturaExibicaoPx,
  };
}

/**
 * Quando dois blocos se tocam ou se sobrepõem **na tela** (por duração curta + altura mínima,
 * ou por horários colados), reparte a largura da coluna em faixas horizontais.
 * Não depende de “mesmo horário” no relógio — só do desenho na grade.
 */
export function atribuirFaixasSobrepostasAgenda(
  posicionados: ItemAgendaPosicionado[],
): Map<string, { indice: number; total: number }> {
  if (posicionados.length === 0) {
    return new Map();
  }

  const comIv = posicionados.map((p) => ({
    ...p,
    iv: intervaloVisualPx(p),
  }));

  comIv.sort((a, b) => {
    const d = a.iv.top - b.iv.top;
    if (d !== 0) return d;
    return b.iv.bottom - a.iv.bottom;
  });

  /** Extremo inferior (px) já ocupado na faixa pelo último card colocado. */
  const fimPorFaixa: number[] = [];
  const indicePorId = new Map<string, number>();

  for (const { item, iv } of comIv) {
    let colocada = -1;
    for (let f = 0; f < fimPorFaixa.length; f++) {
      if (iv.top >= fimPorFaixa[f]) {
        colocada = f;
        fimPorFaixa[f] = iv.bottom;
        break;
      }
    }
    if (colocada < 0) {
      colocada = fimPorFaixa.length;
      fimPorFaixa.push(iv.bottom);
    }
    indicePorId.set(item.id, colocada);
  }

  const total = fimPorFaixa.length;
  const resultado = new Map<string, { indice: number; total: number }>();

  for (const { item } of comIv) {
    resultado.set(item.id, { indice: indicePorId.get(item.id) ?? 0, total });
  }

  return resultado;
}
