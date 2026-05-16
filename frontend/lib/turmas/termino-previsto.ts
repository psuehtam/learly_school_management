/** True quando hoje (local) é igual ou posterior à data prevista (yyyy-MM-dd). */
export function passouTerminoPrevisto(dataTerminoPrevistaIso: string | null | undefined): boolean {
  if (!dataTerminoPrevistaIso?.trim()) return false;

  const partes = dataTerminoPrevistaIso.trim().split("-").map(Number);
  if (partes.length !== 3) return false;

  const [y, m, d] = partes;
  if (!y || !m || !d) return false;

  const termino = new Date(y, m - 1, d);
  const hoje = new Date();
  hoje.setHours(0, 0, 0, 0);
  termino.setHours(0, 0, 0, 0);

  return hoje.getTime() >= termino.getTime();
}
