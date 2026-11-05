import { ConsultaIdComponent } from '../consulta-id/consulta-id';
import { ValidacaoComponent } from '../validacao/validacao';
import { Component, ElementRef, OnDestroy, OnInit, ViewChild, signal } from '@angular/core';
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
  imports: [ConsultaIdComponent, ValidacaoComponent, CommonModule, FormsModule, RouterLink],
  templateUrl: './itens.html',
})
export class ItensComponent implements OnInit, OnDestroy {
  itens = signal<Item[]>([]);
  categorias = signal<Categoria[]>([]);
  carregando = signal(true);
  salvando = signal(false);
  erro = signal('');
  sucesso = signal('');
  mostrarFormulario = false;
  detalheItem = signal<Item | null>(null);
  nomeBusca = '';
  categoriaBusca = 0;
  tipoBusca = '';
  dados = new Item();
  produto = new Produto();
  arquivoImagem: File | null = null;
  previaImagem: string | null = null;
  @ViewChild('arquivoImagemInput') arquivoImagemInput?: ElementRef<HTMLInputElement>;

  constructor(
    private service: ItemService,
    private categoriaService: CategoriaService,
  ) {}
  ngOnInit() {
    this.carregar();
  }

  consultarId(id: number) {
    this.erro.set('');
    this.carregando.set(true);
    this.service.buscar(id).subscribe({
      next: (dados) => {
        this.itens.set([dados]);
        this.detalheItem.set(dados);
        this.carregando.set(false);
      },
      error: (erro) => {
        this.itens.set([]);
        this.erro.set(mensagemErro(erro));
        this.carregando.set(false);
      },
    });
  }

  carregar() {
    this.detalheItem.set(null);
    this.carregando.set(true);
    forkJoin({
      itens: this.service.listar({
        nome: this.nomeBusca,
        idCategoria: this.categoriaBusca,
        tipo: this.tipoBusca,
      }),
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
    this.limparArquivoImagem();
    this.dados = new Item();
    this.produto = new Produto();
    this.erro.set('');
    this.sucesso.set('');
    this.mostrarFormulario = true;
  }

  editar(item: Item) {
    this.limparArquivoImagem();
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
      next: (resposta) => {
        // Guarda o ID: uma falha na foto não duplica o produto ao tentar de novo.
        this.dados.idItem = this.dados.idItem || (resposta as Item).idItem;
        if (this.dados.tipoItem === 'P' && this.arquivoImagem) {
          this.service.salvarImagem(this.dados.idItem, this.arquivoImagem).subscribe({
            next: () => this.concluirSalvamento(),
            error: (erro) => {
              this.salvando.set(false);
              this.erro.set(
                'O produto foi salvo, mas a imagem não: ' +
                  mensagemErro(erro) +
                  ' Selecione outra imagem ou tente salvar novamente.',
              );
              this.carregar();
            },
          });
        } else this.concluirSalvamento();
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

  desativar(item: Item) {
    const dados = structuredClone(item);
    dados.ativoProdutoServico = 'N';
    this.salvando.set(true);
    this.erro.set('');
    this.service.alterar(item.idItem, dados).subscribe({
      next: () => {
        this.salvando.set(false);
        this.sucesso.set('Item desativado. O histórico foi preservado.');
        this.carregar();
      },
      error: (erro) => {
        this.salvando.set(false);
        this.erro.set(mensagemErro(erro));
      },
    });
  }
  selecionarImagem(evento: Event) {
    const campo = evento.target as HTMLInputElement;
    const arquivo = campo.files?.[0];
    this.limparArquivoImagem(false);
    if (!arquivo) return;
    if (
      !['image/png', 'image/jpeg', 'image/webp'].includes(arquivo.type) ||
      arquivo.size > 2 * 1024 * 1024 ||
      arquivo.size === 0
    ) {
      this.erro.set('Selecione uma imagem PNG, JPEG ou WebP de até 2 MiB.');
      campo.value = '';
      return;
    }
    this.erro.set('');
    this.arquivoImagem = arquivo;
    this.previaImagem = URL.createObjectURL(arquivo);
  }

  limparArquivoImagem(limparCampo = true) {
    if (this.previaImagem) URL.revokeObjectURL(this.previaImagem);
    this.previaImagem = null;
    this.arquivoImagem = null;
    if (limparCampo && this.arquivoImagemInput) this.arquivoImagemInput.nativeElement.value = '';
  }

  removerImagem() {
    if (!this.dados.idItem || this.salvando()) return;
    this.salvando.set(true);
    this.erro.set('');
    this.service.removerImagem(this.dados.idItem).subscribe({
      next: () => {
        this.dados.imagemUrl = null;
        this.limparArquivoImagem();
        this.salvando.set(false);
        this.sucesso.set('Imagem removida.');
        this.carregar();
      },
      error: (erro) => {
        this.salvando.set(false);
        this.erro.set(mensagemErro(erro));
      },
    });
  }

  private concluirSalvamento() {
    this.limparArquivoImagem();
    this.salvando.set(false);
    this.mostrarFormulario = false;
    this.sucesso.set('Item salvo com sucesso.');
    this.carregar();
  }

  ngOnDestroy() {
    this.limparArquivoImagem();
  }
}
