import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Observable } from 'rxjs';
import { Categoria } from '../../models/categoria';
import { CategoriaService } from '../../services/categoria-service';
import { mensagemErro } from '../../services/erro-api';

@Component({ selector: 'app-categorias', imports: [FormsModule], templateUrl: './categorias.html' })
export class CategoriasComponent implements OnInit {
  categorias = signal<Categoria[]>([]);
  carregando = signal(true);
  salvando = signal(false);
  erro = signal('');
  sucesso = signal('');
  mostrarFormulario = false;
  dados = new Categoria();

  constructor(private service: CategoriaService) {}
  ngOnInit() {
    this.carregar();
  }

  carregar() {
    this.carregando.set(true);
    this.service.listar().subscribe({
      next: (dados) => {
        this.categorias.set(dados);
        this.carregando.set(false);
      },
      error: (erro) => {
        this.erro.set(mensagemErro(erro));
        this.carregando.set(false);
      },
    });
  }

  nova() {
    this.dados = new Categoria();
    this.erro.set('');
    this.sucesso.set('');
    this.mostrarFormulario = true;
  }

  editar(categoria: Categoria) {
    this.dados = structuredClone(categoria);
    this.erro.set('');
    this.sucesso.set('');
    this.mostrarFormulario = true;
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  salvar() {
    if (!this.dados.categoria.trim() || this.salvando()) {
      return;
    }
    this.dados.categoria = this.dados.categoria.trim();
    this.erro.set('');
    this.salvando.set(true);
    const operacao: Observable<unknown> = this.dados.idcategoria
      ? this.service.alterar(this.dados.idcategoria, this.dados)
      : this.service.adicionar(this.dados);
    operacao.subscribe({
      next: () => {
        this.salvando.set(false);
        this.mostrarFormulario = false;
        this.sucesso.set('Categoria salva com sucesso.');
        this.carregar();
      },
      error: (erro) => {
        this.erro.set(mensagemErro(erro));
        this.salvando.set(false);
      },
    });
  }

  remover(categoria: Categoria) {
    if (!window.confirm(`Excluir a categoria ${categoria.categoria}?`)) {
      return;
    }
    this.erro.set('');
    this.sucesso.set('');
    this.salvando.set(true);
    this.service.remover(categoria.idcategoria).subscribe({
      next: () => {
        this.salvando.set(false);
        this.sucesso.set('Categoria excluída.');
        this.carregar();
      },
      error: (erro) => {
        this.erro.set(mensagemErro(erro));
        this.salvando.set(false);
      },
    });
  }
}
