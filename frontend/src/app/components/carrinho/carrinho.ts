import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { CarrinhoService, LinhaCarrinho } from '../../services/carrinho-service';
import { ItemService } from '../../services/item-service';
import { SessaoService } from '../../services/sessao-service';
import { LojaService } from '../../services/loja-service';
import { mensagemErro } from '../../services/erro-api';

@Component({
  selector: 'app-carrinho',
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './carrinho.html',
})
export class CarrinhoComponent implements OnInit {
  errosQuantidade = signal<Record<number, string>>({});
  erro = signal('');
  salvando = signal(false);
  estoques = signal<Record<number, number>>({});
  constructor(
    public carrinho: CarrinhoService,
    public sessao: SessaoService,
    private itens: ItemService,
    private loja: LojaService,
    private router: Router,
  ) {}
  ngOnInit() {
    this.itens.listar({ ativo: 'S' }).subscribe({
      next: (itens) => {
        const saldos: Record<number, number> = {};
        for (const item of itens) {
          if (item.tipoItem === 'P') saldos[item.idItem] = item.produto?.quantidadeEstoque || 0;
        }
        this.estoques.set(saldos);
      },
      error: (erro) => this.erro.set(mensagemErro(erro)),
    });
  }
  alterar(item: LinhaCarrinho, quantidade: number) {
    this.erro.set('');
    const erros = { ...this.errosQuantidade() };
    if (!Number.isInteger(quantidade) || quantidade < 1) {
      erros[item.idItem] = 'Use uma quantidade inteira positiva.';
      this.errosQuantidade.set(erros);
      return;
    }
    if (item.tipoItem === 'P' && quantidade > (this.estoques()[item.idItem] || 0)) {
      erros[item.idItem] = 'Quantidade maior que o estoque disponível.';
      this.errosQuantidade.set(erros);
      return;
    }
    delete erros[item.idItem];
    this.errosQuantidade.set(erros);
    this.carrinho.atualizar(item.idItem, quantidade);
  }
  quantidadeInvalida(): boolean {
    return this.carrinho.itens().some((item) => !!this.errosQuantidade()[item.idItem]);
  }
  confirmar() {
    if (this.salvando() || this.quantidadeInvalida() || !this.carrinho.itens().length) return;
    if (!this.sessao.atual()) {
      this.router.navigate(['/loja/identificar'], { queryParams: { retorno: '/loja/carrinho' } });
      return;
    }
    this.erro.set('');
    this.salvando.set(true);
    const itens = this.carrinho
      .itens()
      .map((item) => ({ idItem: item.idItem, quantidadeItem: item.quantidadeItem }));
    this.loja.confirmar(itens).subscribe({
      next: (pedido) => {
        this.carrinho.limpar();
        this.salvando.set(false);
        this.router.navigate(['/loja/pedidos', pedido.idAtendimento], {
          queryParams: { confirmado: '1' },
        });
      },
      error: (erro) => {
        this.erro.set(mensagemErro(erro));
        this.salvando.set(false);
        if (erro.status === 401) this.sessao.atual.set(null);
      },
    });
  }
}
