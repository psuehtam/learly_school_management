import { redirect } from "next/navigation";

/** Mantido para links antigos; a tela principal passou a ser `/horarios-funcionamento`. */
export default function ConfiguracoesPage() {
  redirect("/horarios-funcionamento");
}
