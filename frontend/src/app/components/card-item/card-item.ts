import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Item } from '../../models/item';

@Component({
  selector: 'app-card-item',
  imports: [CommonModule, RouterLink],
  template: ` <article class="produto-card">
    <a
      class="item-simbolo"
      [class.servico]="item.tipoItem === 'S'"
      [routerLink]="['/loja/itens', item.idItem]"
      [attr.aria-label]="'Ver ' + item.item"
      >{{ item.tipoItem === 'P' ? 'P' : 'S' }}</a
    >
    <div class="produto-conteudo">
      <span class="etiqueta">{{ item.categoria }}</span>
      <h2>
        <a [routerLink]="['/loja/itens', item.idItem]">{{ item.item }}</a>
      </h2>
      <p class="ajuda">{{ item.descricaoItem || 'Conheça este item da nossa loja.' }}</p>
      <p class="preco-loja">{{ item.produto?.precoVenda ?? item.preco | currency: 'BRL' }}</p>
      <p class="ajuda">
        {{
          item.tipoItem === 'S'
            ? 'Serviço disponível'
            : (item.produto?.quantidadeEstoque ?? 0) + ' unidades disponíveis'
        }}
      </p>
      <button
        (click)="adicionar.emit(item)"
        [disabled]="item.tipoItem === 'P' && (item.produto?.quantidadeEstoque ?? 0) <= 0"
        [attr.aria-label]="'Adicionar ' + item.item"
      >
        {{
          item.tipoItem === 'P' && (item.produto?.quantidadeEstoque ?? 0) <= 0
            ? 'Sem estoque'
            : 'Adicionar ao carrinho'
        }}
      </button>
    </div>
  </article>`,
})
export class CardItemComponent {
  @Input({ required: true }) item!: Item;
  @Output() adicionar = new EventEmitter<Item>();
}
