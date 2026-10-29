import { Component, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Pessoa } from '../../models/pessoa';
import { Endereco } from '../../models/endereco';
import { PessoaService } from '../../services/pessoa-service';
import { SessaoService } from '../../services/sessao-service';
import { mensagemErro } from '../../services/erro-api';
import { ValidacaoComponent } from '../validacao/validacao';

@Component({
  selector: 'app-cadastro-cliente',
  imports: [FormsModule, RouterLink, ValidacaoComponent],
  templateUrl: './cadastro-cliente.html',
})
export class CadastroClienteComponent {
  dados = new Pessoa();
  cpf = '';
  telefone = '';
  erro = signal('');
  sucesso = signal('');
  salvando = signal(false);
  constructor(
    private pessoas: PessoaService,
    private sessao: SessaoService,
    private router: Router,
  ) {}
  adicionarEndereco() {
    this.dados.enderecos.push(new Endereco());
  }
  removerEndereco(indice: number) {
    if (this.dados.enderecos.length > 1) this.dados.enderecos.splice(indice, 1);
  }
  limpar() {
    this.dados = new Pessoa();
    this.cpf = '';
    this.telefone = '';
    this.erro.set('');
  }
  salvar() {
    if (this.salvando()) return;
    this.dados.cpf = Number(this.cpf);
    this.dados.telefone = this.telefone ? Number(this.telefone) : null;
    this.dados.nome = this.dados.nome.trim();
    this.dados.email = this.dados.email?.trim() || null;
    this.salvando.set(true);
    this.erro.set('');
    this.pessoas.adicionar(this.dados).subscribe({
      next: () => {
        this.sessao.identificar(this.dados.cpf, this.dados.dataNascimento).subscribe({
          next: () => {
            this.salvando.set(false);
            this.router.navigate(['/loja/carrinho']);
          },
          error: () => {
            this.salvando.set(false);
            this.sucesso.set('Cadastro salvo. Identifique-se para continuar.');
          },
        });
      },
      error: (erro) => {
        this.erro.set(mensagemErro(erro));
        this.salvando.set(false);
      },
    });
  }
}
