import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { forkJoin, Observable } from 'rxjs';
import { Item, Produto } from '../../models/item';
import { Categoria } from '../../models/categoria';
import { ItemService } from '../../services/item-service';
import { CategoriaService } from '../../services/categoria-service';
import { mensagemErro } from '../../services/erro-api';

@Component({
  selector: 'app-itens',
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './itens.html',
})
export class ItensComponent implements OnInit {
  itens = signal<Item[]>([]);
  categorias = signal<Categoria[]>([]);
  carregando = signal(true);
  salvando = signal(false);
  erro = signal('');
  sucesso = signal('');
  mostrarFormulario = false;
  dados = new Item();
  produto = new Produto();

  constructor(
    private service: ItemService,
    private categoriaService: CategoriaService,
  ) {}
  ngOnInit() {
    this.carregar();
  }

  carregar() {
    this.carregando.set(true);
    forkJoin({
      itens: this.service.listar(),
      categorias: this.categoriaService.listar(),
    }).subscribe({
      next: (dados) => {
        this.itens.set(dados.itens);
        this.categorias.set(dados.categorias);
        this.carregando.set(false);
      },
      error: (erro) => {
        this.erro.set(mensagemErro(erro));
        this.carregando.set(false);
      },
    });
  }

  novo() {
    this.dados = new Item();
    this.produto = new Produto();
    this.erro.set('');
    this.sucesso.set('');
    this.mostrarFormulario = true;
  }

  editar(item: Item) {
    this.dados = structuredClone(item);
    this.produto = item.produto ? structuredClone(item.produto) : new Produto();
    this.erro.set('');
    this.sucesso.set('');
    this.mostrarFormulario = true;
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  salvar() {
    if (this.salvando()) {
      return;
    }
    this.erro.set('');
    if (!this.dados.idCategoria) {
      this.erro.set('Selecione uma categoria.');
      return;
    }
    this.dados.item = this.dados.item.trim();
    this.dados.produto = this.dados.tipoItem === 'P' ? this.produto : null;
    this.salvando.set(true);
    const operacao: Observable<unknown> = this.dados.idItem
      ? this.service.alterar(this.dados.idItem, this.dados)
      : this.service.adicionar(this.dados);
    operacao.subscribe({
      next: () => {
        this.salvando.set(false);
        this.mostrarFormulario = false;
        this.sucesso.set('Item salvo com sucesso.');
        this.carregar();
      },
      error: (erro) => {
        this.erro.set(mensagemErro(erro));
        this.salvando.set(false);
      },
    });
  }

  remover(item: Item) {
    if (
      !window.confirm(
        `Excluir ${item.item}? Itens com movimentação devem ser desativados, preservando o histórico.`,
      )
    ) {
      return;
    }
    this.erro.set('');
    this.sucesso.set('');
    this.salvando.set(true);
    this.service.remover(item.idItem).subscribe({
      next: () => {
        this.salvando.set(false);
        this.sucesso.set('Item excluído.');
        this.carregar();
      },
      error: (erro) => {
        this.erro.set(mensagemErro(erro));
        this.salvando.set(false);
      },
    });
  }
}
