import { Component, signal } from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterLinkActive } from '@angular/router';
import { SessaoService } from '../../services/sessao-service';
import { CarrinhoService } from '../../services/carrinho-service';
import { mensagemErro } from '../../services/erro-api';

@Component({
  selector: 'app-menu',
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './menu.html',
})
export class MenuComponent {
  url = signal('');
  erro = signal('');
  constructor(
    public sessao: SessaoService,
    public carrinho: CarrinhoService,
    private router: Router,
  ) {
    this.url.set(router.url);
    router.events.subscribe((evento) => {
      if (evento instanceof NavigationEnd) this.url.set(evento.urlAfterRedirects);
    });
    sessao.verificar().subscribe({ error: () => sessao.atual.set(null) });
  }
  sair() {
    this.sessao.sair().subscribe({
      next: () => {
        this.erro.set('');
        this.router.navigate(['/loja']);
      },
      error: (erro) => this.erro.set(mensagemErro(erro)),
    });
  }
}
