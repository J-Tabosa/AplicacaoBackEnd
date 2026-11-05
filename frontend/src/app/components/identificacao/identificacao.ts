import { Component, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { SessaoService } from '../../services/sessao-service';
import { mensagemErro } from '../../services/erro-api';
import { ValidacaoComponent } from '../validacao/validacao';

@Component({
  selector: 'app-identificacao',
  imports: [FormsModule, RouterLink, ValidacaoComponent],
  template: ` <section class="painel identificacao">
    <span class="etiqueta">SEU ACESSO</span>
    <h1>Identifique-se</h1>
    <p>Entre com seu e-mail ou CPF e a senha cadastrada.</p>
    @if (erro()) {
      <div class="mensagem erro" role="alert">{{ erro() }}</div>
    }
    <form #form="ngForm" (ngSubmit)="entrar()">
      <app-validacao [form]="form"></app-validacao>
      <fieldset [disabled]="carregando()">
        <div class="grade-form">
          <div class="campo inteiro">
            <label for="login">E-mail ou CPF *</label>
            <input
              id="login"
              name="login"
              [(ngModel)]="login"
              required
              maxlength="254"
              autocomplete="username"
            />
          </div>
          <div class="campo inteiro">
            <label for="senha">Senha *</label>
            <input
              type="password"
              id="senha"
              name="senha"
              [(ngModel)]="senha"
              required
              maxlength="128"
              autocomplete="current-password"
            />
          </div>
        </div>
        <div class="acoes acoes-form">
          <button type="submit" [disabled]="form.invalid || carregando()">
            {{ carregando() ? 'Entrando…' : 'Entrar' }}</button
          ><a routerLink="/loja/cadastro">Criar meu cadastro</a>
        </div>
      </fieldset>
    </form>
    <p class="ajuda">
      Funcionários entram com e-mail ou CPF e senha para acessar o painel administrativo.
    </p>
  </section>`,
})
export class IdentificacaoComponent {
  login = '';
  senha = '';
  erro = signal('');
  carregando = signal(false);
  constructor(
    private sessao: SessaoService,
    private router: Router,
    private rota: ActivatedRoute,
  ) {}
  entrar() {
    if (!this.login.trim() || !this.senha || this.carregando()) return;
    this.erro.set('');
    this.carregando.set(true);
    this.sessao.identificar(this.login.trim(), this.senha).subscribe({
      next: (usuario) => {
        this.senha = '';
        this.carregando.set(false);
        const retorno = this.rota.snapshot.queryParamMap.get('retorno');
        if (retorno && retorno.startsWith('/') && !retorno.startsWith('//')) {
          if (!usuario.funcionario && !retorno.startsWith('/loja')) {
            this.erro.set('Este cadastro é de cliente. O painel é restrito à equipe.');
            return;
          }
          this.router.navigateByUrl(retorno);
        } else {
          this.router.navigate(['/loja']);
        }
      },
      error: (erro) => {
        this.erro.set(erro.error?.mensagem || mensagemErro(erro));
        this.senha = '';
        this.carregando.set(false);
      },
    });
  }
}
