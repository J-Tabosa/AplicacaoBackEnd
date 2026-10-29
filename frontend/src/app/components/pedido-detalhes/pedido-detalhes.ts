import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Atendimento } from '../../models/atendimento';
import { nomeStatus } from '../../models/status';

@Component({
  selector: 'app-pedido-detalhes',
  imports: [CommonModule],
  template: ` <h2>Pedido #{{ pedido.idAtendimento }}</h2>
    <p>
      <strong>Cliente #{{ pedido.idPessoa }}:</strong> {{ pedido.cliente }}
    </p>
    <p>
      <strong>Data:</strong> {{ pedido.dataAtendimento | date: 'dd/MM/yyyy' : 'UTC' }} ·
      <strong>Situação:</strong> {{ nomeStatus(pedido.statusAtendimento) }}
    </p>
    @for (endereco of pedido.pessoa.enderecos; track endereco.idendereco) {
      <p class="ajuda">
        CEP {{ endereco.cep }} · Nº {{ endereco.numero || 's/n' }} {{ endereco.complemento }}
      </p>
    }
    <div class="tabela-responsiva">
      <table>
        <thead>
          <tr>
            <th>Item</th>
            <th>Funcionário / cargo</th>
            <th>Quantidade</th>
            <th>Preço registrado</th>
            <th>Subtotal</th>
          </tr>
        </thead>
        <tbody>
          @for (item of pedido.itens; track item.idItem) {
            <tr>
              <td>{{ item.item }}</td>
              <td>
                {{ item.funcionario }}<br /><small>{{ item.cargoFuncionario }}</small>
              </td>
              <td>{{ item.quantidadeItem }}</td>
              <td>{{ item.valorUnitarioEntrada | currency: 'BRL' }}</td>
              <td>{{ item.subtotal | currency: 'BRL' }}</td>
            </tr>
          }
        </tbody>
      </table>
    </div>
    <div class="total">Total: {{ pedido.valorTotal | currency: 'BRL' }}</div>`,
})
export class PedidoDetalhesComponent {
  @Input({ required: true }) pedido!: Atendimento;
  nomeStatus = nomeStatus;
}
