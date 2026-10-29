import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { SessaoService } from './sessao-service';

export const acessoEquipe: CanActivateFn = (_route, state) => {
  const sessao = inject(SessaoService);
  const router = inject(Router);
  return sessao.verificar().pipe(
    map((cliente) =>
      cliente.funcionario
        ? true
        : router.createUrlTree(['/loja/identificar'], { queryParams: { retorno: state.url } }),
    ),
    catchError(() => {
      sessao.atual.set(null);
      return of(
        router.createUrlTree(['/loja/identificar'], { queryParams: { retorno: state.url } }),
      );
    }),
  );
};
