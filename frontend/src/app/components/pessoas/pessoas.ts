import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { Observable } from 'rxjs';
import { Pessoa } from '../../models/pessoa';
import { Funcionario } from '../../models/funcionario';
import { Endereco } from '../../models/endereco';
import { PessoaService } from '../../services/pessoa-service';
import { FuncionarioService } from '../../services/funcionario-service';
import { mensagemErro } from '../../services/erro-api';

@Component({
  selector: 'app-pessoas',
  imports: [CommonModule, FormsModule],
  templateUrl: './pessoas.html',
})
export class PessoasComponent implements OnInit {
  lista = signal<(Pessoa | Funcionario)[]>([]);
  carregando = signal(true);
  salvando = signal(false);
  erro = signal('');
  sucesso = signal('');
  funcionario = false;
  mostrarFormulario = false;
  dados = new Pessoa();
  cpf = '';
  telefone = '';
  cargo = '';
  dataAdmissao = '';

  constructor(
    private pessoas: PessoaService,
    private funcionarios: FuncionarioService,
    private rota: ActivatedRoute,
  ) {}

  ngOnInit() {
    this.funcionario = this.rota.snapshot.data['funcionario'] === true;
    this.carregar();
  }

  carregar() {
    this.carregando.set(true);
    const consulta = this.funcionario ? this.funcionarios.listar() : this.pessoas.listar();
    consulta.subscribe({
      next: (dados) => {
        this.lista.set(dados);
        this.carregando.set(false);
      },
      error: (erro) => {
        this.erro.set(mensagemErro(erro));
        this.carregando.set(false);
      },
    });
  }

  nova() {
    this.dados = new Pessoa();
    this.cpf = '';
    this.telefone = '';
    this.cargo = '';
    this.dataAdmissao = '';
    this.erro.set('');
    this.sucesso.set('');
    this.mostrarFormulario = true;
  }

  editar(pessoa: Pessoa | Funcionario) {
    this.dados = structuredClone(pessoa);
    this.cpf = String(pessoa.cpf).padStart(11, '0');
    this.telefone = pessoa.telefone === null ? '' : String(pessoa.telefone);
    if ('cargo' in pessoa) {
      this.cargo = pessoa.cargo;
      this.dataAdmissao = pessoa.dataAdmissao;
    }
    this.erro.set('');
    this.sucesso.set('');
    this.mostrarFormulario = true;
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  cargoDe(pessoa: Pessoa | Funcionario): string {
    if ('cargo' in pessoa) {
      return pessoa.cargo;
    }
    return '';
  }

  adicionarEndereco() {
    this.dados.enderecos.push(new Endereco());
  }

  removerEndereco(indice: number) {
    if (this.dados.enderecos.length > 1) {
      this.dados.enderecos.splice(indice, 1);
    }
  }

  salvar() {
    if (this.salvando()) {
      return;
    }
    this.erro.set('');
    if (!/^\d{11}$/.test(this.cpf) || Number(this.cpf) === 0) {
      this.erro.set('Informe o CPF com 11 dígitos, sem pontuação.');
      return;
    }
    if (this.telefone && !/^\d{8,13}$/.test(this.telefone)) {
      this.erro.set('Informe o telefone somente com números, incluindo o DDD.');
      return;
    }
    this.dados.nome = this.dados.nome.trim();
    this.dados.cpf = Number(this.cpf);
    this.dados.telefone = this.telefone ? Number(this.telefone) : null;
    this.dados.email = this.dados.email?.trim() || null;
    this.salvando.set(true);

    let operacao: Observable<unknown>;
    if (this.funcionario) {
      const cadastro: Funcionario = {
        ...this.dados,
        cargo: this.cargo.trim(),
        dataAdmissao: this.dataAdmissao,
      };
      operacao = this.dados.idpessoa
        ? this.funcionarios.alterar(this.dados.idpessoa, cadastro)
        : this.funcionarios.adicionar(cadastro);
    } else {
      operacao = this.dados.idpessoa
        ? this.pessoas.alterar(this.dados.idpessoa, this.dados)
        : this.pessoas.adicionar(this.dados);
    }

    operacao.subscribe({
      next: () => {
        this.salvando.set(false);
        this.mostrarFormulario = false;
        this.sucesso.set('Cadastro salvo com sucesso.');
        this.carregar();
      },
      error: (erro) => {
        this.erro.set(mensagemErro(erro));
        this.salvando.set(false);
      },
    });
  }

  remover(pessoa: Pessoa | Funcionario) {
    const pergunta = this.funcionario
      ? `Excluir o vínculo de funcionário de ${pessoa.nome}? A pessoa e seus endereços serão preservados.`
      : `Excluir ${pessoa.nome} e seus endereços?`;
    if (!window.confirm(pergunta)) {
      return;
    }
    this.erro.set('');
    this.sucesso.set('');
    this.salvando.set(true);
    const operacao = this.funcionario
      ? this.funcionarios.remover(pessoa.idpessoa)
      : this.pessoas.remover(pessoa.idpessoa);
    operacao.subscribe({
      next: () => {
        this.salvando.set(false);
        this.sucesso.set(
          this.funcionario
            ? 'Vínculo excluído. A pessoa e os endereços continuam cadastrados.'
            : 'Pessoa excluída.',
        );
        this.carregar();
      },
      error: (erro) => {
        this.erro.set(mensagemErro(erro));
        this.salvando.set(false);
      },
    });
  }
}
