import { EstoqueService } from '../../services/estoque-service';
import { Component, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { PessoaService } from '../../services/pessoa-service';
import { FuncionarioService } from '../../services/funcionario-service';
import { CategoriaService } from '../../services/categoria-service';
import { ItemService } from '../../services/item-service';
import { EntradaService } from '../../services/entrada-service';
import { AtendimentoService } from '../../services/atendimento-service';
import { mensagemErro } from '../../services/erro-api';

@Component({ selector: 'app-home', imports: [RouterLink], templateUrl: './home.html' })
export class HomeComponent implements OnInit {
  carregando = signal(true);
  erro = signal('');
  totais = signal({
    pessoas: 0,
    funcionarios: 0,
    categorias: 0,
    itens: 0,
    produtos: 0,
    servicos: 0,
    estoque: 0,
    abertos: 0,
    entradas: 0,
    atendimentos: 0,
  });

  constructor(
    private pessoas: PessoaService,
    private funcionarios: FuncionarioService,
    private categorias: CategoriaService,
    private itens: ItemService,
    private entradas: EntradaService,
    private estoque: EstoqueService,
    private atendimentos: AtendimentoService,
  ) {}

  ngOnInit() {
    this.carregar();
  }

  carregar() {
    this.carregando.set(true);
    this.erro.set('');
    forkJoin({
      pessoas: this.pessoas.listar(),
      funcionarios: this.funcionarios.listar(),
      categorias: this.categorias.listar(),
      itens: this.itens.listar(),
      entradas: this.entradas.listar(),
      estoque: this.estoque.listar(),
      atendimentos: this.atendimentos.listar(),
    }).subscribe({
      next: (dados) => {
        this.totais.set({
          pessoas: dados.pessoas.length,
          funcionarios: dados.funcionarios.length,
          categorias: dados.categorias.length,
          itens: dados.itens.length,
          produtos: dados.itens.filter((i) => i.tipoItem === 'P').length,
          servicos: dados.itens.filter((i) => i.tipoItem === 'S').length,
          estoque: dados.estoque.reduce((s, i) => s + i.quantidadeEstoque, 0),
          abertos: dados.atendimentos.filter((a) => a.statusAtendimento === 'A').length,
          entradas: dados.entradas.length,
          atendimentos: dados.atendimentos.length,
        });
        this.carregando.set(false);
      },
      error: (erro) => {
        this.erro.set(mensagemErro(erro));
        this.carregando.set(false);
      },
    });
  }
}
