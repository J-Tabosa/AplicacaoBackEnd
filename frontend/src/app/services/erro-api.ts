import { HttpErrorResponse } from '@angular/common/http';

export function mensagemErro(erro: HttpErrorResponse): string {
  if (erro.status === 0 || erro.status === 502 || erro.status === 504) {
    return 'Não foi possível acessar a API. Confira se o backend está em execução e tente novamente.';
  }
  if (erro.status === 401) return 'Identifique-se para continuar.';
  if (erro.status === 403) return 'Acesso restrito aos funcionários.';
  if (erro.error?.mensagem) {
    const detalhe = erro.error.detalhe ? ` ${erro.error.detalhe}` : '';
    return erro.error.mensagem + detalhe;
  }
  if (erro.error?.errors) {
    return Object.values(erro.error.errors).flat().join(' ');
  }
  return 'Não foi possível concluir a operação. Tente novamente.';
}
