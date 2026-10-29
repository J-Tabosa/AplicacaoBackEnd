import { computed, Injectable, signal } from '@angular/core';
import { Item } from '../models/item';

export interface LinhaCarrinho {
  idItem: number;
  item: string;
  tipoItem: string;
  quantidadeItem: number;
  preco: number;
}

@Injectable({ providedIn: 'root' })
export class CarrinhoService {
  itens = signal<LinhaCarrinho[]>([]);
  total = computed(() =>
    this.itens().reduce((soma, item) => soma + item.preco * item.quantidadeItem, 0),
  );
  quantidade = computed(() => this.itens().reduce((soma, item) => soma + item.quantidadeItem, 0));
  private chave = 'estetica.carrinho';

  constructor() {
    try {
      const salvos = JSON.parse(localStorage.getItem(this.chave) || '[]');
      if (Array.isArray(salvos)) {
        this.itens.set(
          salvos.filter(
            (item) =>
              Number.isInteger(item.idItem) &&
              item.idItem > 0 &&
              Number.isInteger(item.quantidadeItem) &&
              item.quantidadeItem > 0 &&
              typeof item.preco === 'number' &&
              item.preco >= 0 &&
              typeof item.item === 'string',
          ),
        );
      }
    } catch {
      this.itens.set([]);
    }
  }

  adicionar(item: Item): string {
    const lista = structuredClone(this.itens());
    const existente = lista.find((linha) => linha.idItem === item.idItem);
    const quantidade = (existente?.quantidadeItem || 0) + 1;
    if (item.tipoItem === 'P' && (item.produto?.quantidadeEstoque ?? 0) < quantidade) {
      return 'A quantidade selecionada ultrapassa o estoque disponível.';
    }
    if (existente) {
      existente.quantidadeItem = quantidade;
    } else {
      lista.push({
        idItem: item.idItem,
        item: item.item,
        tipoItem: item.tipoItem,
        quantidadeItem: 1,
        preco: item.produto?.precoVenda ?? item.preco,
      });
    }
    this.salvar(lista);
    return '';
  }

  atualizar(id: number, quantidade: number) {
    if (!Number.isInteger(quantidade) || quantidade < 1) return;
    this.salvar(
      this.itens().map((item) =>
        item.idItem === id ? { ...item, quantidadeItem: quantidade } : item,
      ),
    );
  }
  remover(id: number) {
    this.salvar(this.itens().filter((item) => item.idItem !== id));
  }
  limpar() {
    this.salvar([]);
  }
  private salvar(lista: LinhaCarrinho[]) {
    this.itens.set(lista);
    try {
      localStorage.setItem(this.chave, JSON.stringify(lista));
    } catch {
      /* O carrinho continua em memória. */
    }
  }
}
