import { nomeStatus } from '../../models/status';
import { PedidoDetalhesComponent } from '../pedido-detalhes/pedido-detalhes';
import { ConsultaIdComponent } from '../consulta-id/consulta-id';
import { ValidacaoComponent } from '../validacao/validacao';
import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { forkJoin, Observable } from 'rxjs';
import { Atendimento, AtendimentoItem } from '../../models/atendimento';
import { Pessoa } from '../../models/pessoa';
import { Funcionario } from '../../models/funcionario';
import { Item } from '../../models/item';
import { AtendimentoService } from '../../services/atendimento-service';
import { PessoaService } from '../../services/pessoa-service';
import { FuncionarioService } from '../../services/funcionario-service';
import { ItemService } from '../../services/item-service';
import { mensagemErro } from '../../services/erro-api';

@Component({
  selector: 'app-atendimentos',
  imports: [
    PedidoDetalhesComponent,
    ConsultaIdComponent,
    ValidacaoComponent,
    CommonModule,
    FormsModule,
    RouterLink,
  ],
  templateUrl: './atendimentos.html',
})
export class AtendimentosComponent implements OnInit {
  atendimentos = signal<Atendimento[]>([]);
  pessoas = signal<Pessoa[]>([]);
  funcionarios = signal<Funcionario[]>([]);
  itens = signal<Item[]>([]);
  detalhes = signal<Atendimento | null>(null);
  carregando = signal(true);
  salvando = signal(false);
  erro = signal('');
  sucesso = signal('');
  mostrarFormulario = false;
  statusBusca = '';
  nomeStatus = nomeStatus;
  dados = new Atendimento();

  constructor(
    private service: AtendimentoService,
    private pessoaService: PessoaService,
    private funcionarioService: FuncionarioService,
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
        this.atendimentos.set([dados]);
        this.detalhes.set(dados);
        this.carregando.set(false);
      },
      error: (erro) => {
        this.atendimentos.set([]);
        this.erro.set(mensagemErro(erro));
        this.carregando.set(false);
      },
    });
  }

  carregar() {
    this.detalhes.set(null);
    this.carregando.set(true);
    forkJoin({
      atendimentos: this.service.listar(this.statusBusca),
      pessoas: this.pessoaService.listar(),
      funcionarios: this.funcionarioService.listar(),
      itens: this.itemService.listar(),
    }).subscribe({
      next: (dados) => {
        this.atendimentos.set(dados.atendimentos);
        this.pessoas.set(dados.pessoas);
        this.funcionarios.set(dados.funcionarios);
        this.itens.set(dados.itens);
        this.carregando.set(false);
      },
      error: (erro) => {
        this.erro.set(mensagemErro(erro));
        this.carregando.set(false);
      },
    });
  }

  novo() {
    this.dados = new Atendimento();
    this.detalhes.set(null);
    this.erro.set('');
    this.sucesso.set('');
    this.mostrarFormulario = true;
  }

  editar(atendimento: Atendimento) {
    this.dados = structuredClone(atendimento);
    this.detalhes.set(null);
    this.erro.set('');
    this.sucesso.set('');
    this.mostrarFormulario = true;
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  verDetalhes(atendimento: Atendimento) {
    this.service.buscar(atendimento.idAtendimento).subscribe({
      next: (dados) => {
        this.mostrarFormulario = false;
        this.detalhes.set(dados);
        window.scrollTo({ top: 0, behavior: 'smooth' });
      },
      error: (erro) => this.erro.set(mensagemErro(erro)),
    });
  }

  adicionarItem() {
    this.dados.itens.push(new AtendimentoItem());
  }

  removerItem(indice: number) {
    if (this.dados.itens.length > 1) {
      this.dados.itens.splice(indice, 1);
    }
  }

  itemAlterado(linha: AtendimentoItem) {
    const item = this.itens().find((item) => item.idItem === linha.idItem);
    if (item) {
      linha.valorUnitarioEntrada = item.produto?.precoVenda ?? item.preco;
    }
  }

  calcularTotal(): number {
    let total = 0;
    for (const item of this.dados.itens) {
      total += (item.quantidadeItem || 0) * (item.valorUnitarioEntrada || 0);
    }
    return total;
  }

  salvar() {
    if (this.salvando()) {
      return;
    }
    this.erro.set('');
    if (!this.dados.idPessoa) {
      this.erro.set('Selecione o cliente.');
      return;
    }
    const ids: number[] = [];
    for (const item of this.dados.itens) {
      if (
        !item.idItem ||
        !item.idFuncionario ||
        !Number.isInteger(item.quantidadeItem) ||
        item.quantidadeItem <= 0
      ) {
        this.erro.set(
          'Selecione o item, o funcionário e uma quantidade inteira positiva em cada linha.',
        );
        return;
      }
      if (ids.includes(item.idItem)) {
        this.erro.set('Não repita o mesmo item. Some as quantidades em uma única linha.');
        return;
      }
      ids.push(item.idItem);
    }
    this.salvando.set(true);
    const operacao: Observable<unknown> = this.dados.idAtendimento
      ? this.service.alterar(this.dados.idAtendimento, this.dados)
      : this.service.adicionar(this.dados);
    operacao.subscribe({
      next: () => {
        this.salvando.set(false);
        this.mostrarFormulario = false;
        this.sucesso.set('Atendimento salvo e estoque atualizado.');
        this.carregar();
      },
      error: (erro) => {
        this.erro.set(mensagemErro(erro));
        this.salvando.set(false);
      },
    });
  }

  remover(atendimento: Atendimento) {
    if (
      !window.confirm(
        `Excluir o atendimento de ${atendimento.cliente}? Os produtos serão devolvidos ao estoque.`,
      )
    ) {
      return;
    }
    this.erro.set('');
    this.sucesso.set('');
    this.salvando.set(true);
    this.service.remover(atendimento.idAtendimento).subscribe({
      next: () => {
        this.salvando.set(false);
        this.detalhes.set(null);
        this.sucesso.set('Atendimento excluído e produtos devolvidos ao estoque.');
        this.carregar();
      },
      error: (erro) => {
        this.erro.set(mensagemErro(erro));
        this.salvando.set(false);
      },
    });
  }

  mudarStatus(pedido: Atendimento, proximo: string) {
    if (
      proximo === 'C' &&
      !window.confirm('Cancelar este pedido e devolver os produtos ao estoque?')
    )
      return;
    this.erro.set('');
    this.salvando.set(true);
    this.service.atualizarStatus(pedido.idAtendimento, proximo).subscribe({
      next: () => {
        this.salvando.set(false);
        this.detalhes.set(null);
        this.sucesso.set('Situação do pedido atualizada.');
        this.carregar();
      },
      error: (erro) => {
        this.salvando.set(false);
        this.erro.set(mensagemErro(erro));
      },
    });
  }
}
