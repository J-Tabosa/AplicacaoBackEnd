import { ConsultaIdComponent } from '../consulta-id/consulta-id';
import { ValidacaoComponent } from '../validacao/validacao';
import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { forkJoin, Observable } from 'rxjs';
import { Entrada } from '../../models/entrada';
import { Item } from '../../models/item';
import { EntradaService } from '../../services/entrada-service';
import { ItemService } from '../../services/item-service';
import { mensagemErro } from '../../services/erro-api';

@Component({
  selector: 'app-entradas',
  imports: [ConsultaIdComponent, ValidacaoComponent, CommonModule, FormsModule, RouterLink],
  templateUrl: './entradas.html',
})
export class EntradasComponent implements OnInit {
  entradas = signal<Entrada[]>([]);
  produtos = signal<Item[]>([]);
  carregando = signal(true);
  salvando = signal(false);
  erro = signal('');
  sucesso = signal('');
  mostrarFormulario = false;
  dados = new Entrada();

  constructor(
    private service: EntradaService,
    private itemService: ItemService,
  ) {}
  ngOnInit() {
    this.carregar();
  }

  consultarId(id: number) {
    this.erro.set('');
    this.carregando.set(true);
    this.service.buscar(id).subscribe({
      next: (dados) => {
        this.entradas.set([dados]);
        this.carregando.set(false);
      },
      error: (erro) => {
        this.entradas.set([]);
        this.erro.set(mensagemErro(erro));
        this.carregando.set(false);
      },
    });
  }

  carregar() {
    this.carregando.set(true);
    forkJoin({ entradas: this.service.listar(), itens: this.itemService.listar() }).subscribe({
      next: (dados) => {
        this.entradas.set(dados.entradas);
        this.produtos.set(
          dados.itens.filter((item) => item.tipoItem === 'P' && item.produto !== null),
        );
        this.carregando.set(false);
      },
      error: (erro) => {
        this.erro.set(mensagemErro(erro));
        this.carregando.set(false);
      },
    });
  }

  nova() {
    this.dados = new Entrada();
    this.erro.set('');
    this.sucesso.set('');
    this.mostrarFormulario = true;
  }

  editar(entrada: Entrada) {
    this.dados = structuredClone(entrada);
    this.erro.set('');
    this.sucesso.set('');
    this.mostrarFormulario = true;
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  produtoAlterado() {
    const item = this.produtos().find((item) => item.idItem === this.dados.idItem);
    if (item) {
      this.dados.unidade = item.produto?.unidade || 'UN';
      this.dados.valorUnitarioEntrada = item.preco;
    }
  }

  salvar() {
    if (this.salvando()) {
      return;
    }
    this.erro.set('');
    if (!this.dados.idItem) {
      this.erro.set('Selecione um produto.');
      return;
    }
    if (!Number.isInteger(this.dados.quantidadeEntrada) || this.dados.quantidadeEntrada <= 0) {
      this.erro.set('Informe uma quantidade positiva em unidades inteiras.');
      return;
    }
    this.salvando.set(true);
    const operacao: Observable<unknown> = this.dados.idEntradaProduto
      ? this.service.alterar(this.dados.idEntradaProduto, this.dados)
      : this.service.adicionar(this.dados);
    operacao.subscribe({
      next: () => {
        this.salvando.set(false);
        this.mostrarFormulario = false;
        this.sucesso.set('Entrada salva e estoque atualizado.');
        this.carregar();
      },
      error: (erro) => {
        this.erro.set(mensagemErro(erro));
        this.salvando.set(false);
      },
    });
  }

  remover(entrada: Entrada) {
    if (
      !window.confirm(
        `Excluir esta entrada de ${entrada.produto}? A quantidade será retirada do estoque.`,
      )
    ) {
      return;
    }
    this.erro.set('');
    this.sucesso.set('');
    this.salvando.set(true);
    this.service.remover(entrada.idEntradaProduto).subscribe({
      next: () => {
        this.salvando.set(false);
        this.sucesso.set('Entrada excluída e estoque ajustado.');
        this.carregar();
      },
      error: (erro) => {
        this.erro.set(mensagemErro(erro));
        this.salvando.set(false);
      },
    });
  }
}
