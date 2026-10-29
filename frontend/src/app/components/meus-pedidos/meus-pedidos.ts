import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Atendimento } from '../../models/atendimento';
import { nomeStatus } from '../../models/status';
import { LojaService } from '../../services/loja-service';
import { mensagemErro } from '../../services/erro-api';
import { PedidoDetalhesComponent } from '../pedido-detalhes/pedido-detalhes';

@Component({
  selector: 'app-meus-pedidos',
  imports: [CommonModule, RouterLink, PedidoDetalhesComponent],
  templateUrl: './meus-pedidos.html',
})
export class MeusPedidosComponent implements OnInit {
  pedidos = signal<Atendimento[]>([]);
  detalhe = signal<Atendimento | null>(null);
  erro = signal('');
  carregando = signal(true);
  confirmado = false;
  nomeStatus = nomeStatus;
  constructor(
    private service: LojaService,
    private rota: ActivatedRoute,
  ) {}
  ngOnInit() {
    this.confirmado = this.rota.snapshot.queryParamMap.get('confirmado') === '1';
    const id = Number(this.rota.snapshot.paramMap.get('id'));
    if (id)
      this.service.meuPedido(id).subscribe({
        next: (pedido) => {
          this.detalhe.set(pedido);
          this.carregando.set(false);
        },
        error: (erro) => {
          this.erro.set(mensagemErro(erro));
          this.carregando.set(false);
        },
      });
    else
      this.service.meusPedidos().subscribe({
        next: (pedidos) => {
          this.pedidos.set(pedidos);
          this.carregando.set(false);
        },
        error: (erro) => {
          this.erro.set(mensagemErro(erro));
          this.carregando.set(false);
        },
      });
  }
}
