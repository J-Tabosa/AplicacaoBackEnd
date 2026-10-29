import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Item } from '../../models/item';
import { ItemService } from '../../services/item-service';
import { CarrinhoService } from '../../services/carrinho-service';
import { mensagemErro } from '../../services/erro-api';

@Component({
  selector: 'app-item-loja',
  imports: [CommonModule, RouterLink],
  template: ` <a routerLink="/loja">← Voltar ao catálogo</a>
    @if (erro()) {
      <div class="mensagem erro" role="alert">{{ erro() }}</div>
    }
    @if (sucesso()) {
      <div class="mensagem sucesso" role="status">
        {{ sucesso() }} <a routerLink="/loja/carrinho">Ver carrinho</a>
      </div>
    }
    @if (item(); as produto) {
      <section class="painel detalhe-produto">
        <div class="item-simbolo" [class.servico]="produto.tipoItem === 'S'">
          {{ produto.tipoItem }}
        </div>
        <div>
          <span class="etiqueta">{{ produto.categoria }} · #{{ produto.idItem }}</span>
          <h1>{{ produto.item }}</h1>
          <p>{{ produto.descricaoItem || 'Sem descrição adicional.' }}</p>
          <p class="preco-loja">
            {{ produto.produto?.precoVenda ?? produto.preco | currency: 'BRL' }}
          </p>
          <p>
            {{
              produto.tipoItem === 'S'
                ? 'Serviço disponível para pedido.'
                : (produto.produto?.quantidadeEstoque ?? 0) + ' unidades disponíveis.'
            }}
          </p>
          <button
            (click)="adicionar(produto)"
            [disabled]="produto.tipoItem === 'P' && (produto.produto?.quantidadeEstoque ?? 0) < 1"
          >
            Adicionar ao carrinho
          </button>
        </div>
      </section>
    } @else if (!erro()) {
      <p class="carregando">Carregando item…</p>
    }`,
})
export class ItemLojaComponent implements OnInit {
  item = signal<Item | null>(null);
  erro = signal('');
  sucesso = signal('');
  constructor(
    private service: ItemService,
    private rota: ActivatedRoute,
    private carrinho: CarrinhoService,
  ) {}
  ngOnInit() {
    this.service.buscar(Number(this.rota.snapshot.paramMap.get('id'))).subscribe({
      next: (item) => {
        if (item.ativoProdutoServico === 'S') this.item.set(item);
        else this.erro.set('Item indisponível.');
      },
      error: (erro) => this.erro.set(mensagemErro(erro)),
    });
  }
  adicionar(item: Item) {
    const erro = this.carrinho.adicionar(item);
    this.erro.set(erro);
    this.sucesso.set(erro ? '' : 'Item adicionado ao carrinho.');
  }
}
