import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Estoque } from '../../models/estoque';
import { EstoqueService } from '../../services/estoque-service';
import { mensagemErro } from '../../services/erro-api';
import { ConsultaIdComponent } from '../consulta-id/consulta-id';

@Component({
  selector: 'app-estoque',
  imports: [CommonModule, ConsultaIdComponent],
  template: ` <span class="etiqueta">ADMINISTRATIVO</span>
    <h1>Consulta de estoque</h1>
    <p>Produtos cadastrados, preços e quantidades disponíveis.</p>
    <app-consulta-id
      [carregando]="carregando()"
      (consultar)="buscar($event)"
      (limpar)="carregar()"
    ></app-consulta-id>
    @if (erro()) {
      <div class="mensagem erro" role="alert">{{ erro() }}</div>
    }
    <section class="painel">
      @if (carregando()) {
        <p class="carregando">Carregando estoque…</p>
      } @else if (!itens().length) {
        <p class="vazio">Nenhum produto encontrado no estoque.</p>
      } @else {
        <div class="tabela-responsiva">
          <table>
            <thead>
              <tr>
                <th>ID / produto</th>
                <th>Descrição</th>
                <th>Categoria</th>
                <th>Preço base</th>
                <th>Preço de venda</th>
                <th>Unidade</th>
                <th>Quantidade</th>
                <th>Ativo</th>
              </tr>
            </thead>
            <tbody>
              @for (item of itens(); track item.idItem) {
                <tr>
                  <td>#{{ item.idItem }} · {{ item.item }}</td>
                  <td>{{ item.descricaoItem || '—' }}</td>
                  <td>{{ item.categoria }}</td>
                  <td>{{ item.preco | currency: 'BRL' }}</td>
                  <td>{{ item.precoVenda | currency: 'BRL' }}</td>
                  <td>{{ item.unidade }}</td>
                  <td>{{ item.quantidadeEstoque }}</td>
                  <td>{{ item.ativoProdutoServico === 'S' ? 'Sim' : 'Não' }}</td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </section>`,
})
export class EstoqueComponent implements OnInit {
  itens = signal<Estoque[]>([]);
  erro = signal('');
  carregando = signal(true);
  constructor(private service: EstoqueService) {}
  ngOnInit() {
    this.carregar();
  }
  carregar() {
    this.erro.set('');
    this.carregando.set(true);
    this.service.listar().subscribe({
      next: (itens) => {
        this.itens.set(itens);
        this.carregando.set(false);
      },
      error: (erro) => {
        this.erro.set(mensagemErro(erro));
        this.carregando.set(false);
      },
    });
  }
  buscar(id: number) {
    this.erro.set('');
    this.carregando.set(true);
    this.service.buscar(id).subscribe({
      next: (item) => {
        this.itens.set([item]);
        this.carregando.set(false);
      },
      error: (erro) => {
        this.itens.set([]);
        this.erro.set(mensagemErro(erro));
        this.carregando.set(false);
      },
    });
  }
}
