export function nomeStatus(status: string | null): string {
  if (status === 'P') return 'Em processamento';
  if (status === 'F') return 'Concluído';
  if (status === 'C') return 'Cancelado';
  return 'Aberto';
}
