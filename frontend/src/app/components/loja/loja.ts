import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Item } from '../../models/item';
import { Categoria } from '../../models/categoria';
import { ItemService } from '../../services/item-service';
import { CategoriaService } from '../../services/categoria-service';
import { CarrinhoService } from '../../services/carrinho-service';
import { mensagemErro } from '../../services/erro-api';
import { CardItemComponent } from '../card-item/card-item';

@Component({
  selector: 'app-loja',
  imports: [FormsModule, RouterLink, CardItemComponent],
  templateUrl: './loja.html',
})
export class LojaComponent implements OnInit {
  itens = signal<Item[]>([]);
  categorias = signal<Categoria[]>([]);
  carregando = signal(true);
  erro = signal('');
  sucesso = signal('');
  nome = '';
  tipo = '';
  idCategoria = 0;
  constructor(
    private service: ItemService,
    private categoriaService: CategoriaService,
    private rota: ActivatedRoute,
    private router: Router,
    private carrinho: CarrinhoService,
  ) {}

  ngOnInit() {
    this.categoriaService.listar().subscribe({
      next: (dados) => this.categorias.set(dados),
      error: (erro) => this.erro.set(mensagemErro(erro)),
    });
    this.rota.queryParamMap.subscribe((params) => {
      this.nome = params.get('nome') || '';
      this.tipo = ['P', 'S'].includes(params.get('tipo') || '') ? params.get('tipo')! : '';
      this.idCategoria = Number(params.get('idCategoria')) || 0;
      this.carregar();
    });
  }
  carregar() {
    this.carregando.set(true);
    this.erro.set('');
    this.service
      .listar({ nome: this.nome, tipo: this.tipo, idCategoria: this.idCategoria, ativo: 'S' })
      .subscribe({
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
  filtrar() {
    this.router.navigate(['/loja'], {
      queryParams: {
        nome: this.nome.trim() || null,
        tipo: this.tipo || null,
        idCategoria: this.idCategoria || null,
      },
    });
  }
  adicionar(item: Item) {
    const erro = this.carrinho.adicionar(item);
    this.erro.set(erro);
    this.sucesso.set(erro ? '' : `${item.item} adicionado ao carrinho.`);
  }
}
