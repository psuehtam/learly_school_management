import type { ReactNode } from "react";
import { Modal } from "@/components/ui/modal";
import type { OcupacaoAgendaItem } from "@/lib/agenda";
import {
  duracaoAgendaMinutos,
  ocupacaoAgendaTipoBadgeClassNames,
  ocupacaoAgendaTipoLegivel,
} from "@/lib/agenda";

export interface AgendaOcupacaoDetalheModalProps {
  open: boolean;
  onClose: () => void;
  item: OcupacaoAgendaItem | null;
  /** Nome do usuário da coluna (professor/participante). */
  usuarioColunaNome?: string;
}

function Linha({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div>
      <dt className="text-xs font-semibold uppercase tracking-wide text-zinc-500">{label}</dt>
      <dd className="mt-0.5 text-sm text-zinc-900">{children}</dd>
    </div>
  );
}

export function AgendaOcupacaoDetalheModal({
  open,
  onClose,
  item,
  usuarioColunaNome,
}: AgendaOcupacaoDetalheModalProps) {
  if (!item) return null;

  const tipo = ocupacaoAgendaTipoLegivel(item.tipo);
  const minutos = duracaoAgendaMinutos(item.inicio, item.fim);

  return (
    <Modal
      open={open}
      onClose={onClose}
      title={item.titulo}
      className="max-w-md"
      footer={
        <button
          type="button"
          onClick={onClose}
          className="h-9 rounded-lg bg-zinc-900 px-4 text-sm font-medium text-white hover:bg-zinc-800"
        >
          Fechar
        </button>
      }
    >
      <div className="space-y-4">
        <div className="flex flex-wrap items-center gap-2">
          <span
            className={`inline-flex rounded-md px-2 py-1 text-xs font-bold ${ocupacaoAgendaTipoBadgeClassNames(item.tipo)}`}
          >
            {tipo}
          </span>
          {item.tipo === "COMPROMISSO" && item.categoriaCompromisso ? (
            <span className="text-sm font-medium text-violet-900">{item.categoriaCompromisso}</span>
          ) : null}
        </div>

        <dl className="space-y-3">
          <Linha label="Horário">
            {item.inicio} – {item.fim}
            <span className="ml-2 text-zinc-500">
              ({minutos === 0 ? "menos de 1 min" : minutos === 1 ? "1 minuto" : `${minutos} minutos`})
            </span>
          </Linha>
          {usuarioColunaNome ? (
            <Linha label="Coluna (usuário)">{usuarioColunaNome}</Linha>
          ) : null}
          {item.subtitulo ? <Linha label="Detalhe">{item.subtitulo}</Linha> : null}
          {item.contextoAulaExtra ? <Linha label="Reposição">{item.contextoAulaExtra}</Linha> : null}
        </dl>
      </div>
    </Modal>
  );
}
