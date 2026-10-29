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
    <p>Use os dados do seu cadastro para continuar.</p>
    @if (erro()) {
      <div class="mensagem erro" role="alert">{{ erro() }}</div>
    }
    <form #form="ngForm" (ngSubmit)="entrar()">
      <app-validacao [form]="form"></app-validacao>
      <fieldset [disabled]="carregando()">
        <div class="grade-form">
          <div class="campo inteiro">
            <label for="cpf">CPF *</label
            ><input
              id="cpf"
              name="cpf"
              [(ngModel)]="cpf"
              required
              pattern="[0-9]{11}"
              maxlength="11"
              inputmode="numeric"
              autocomplete="username"
            />
          </div>
          <div class="campo inteiro">
            <label for="nascimento">Data de nascimento *</label
            ><input
              type="date"
              id="nascimento"
              name="nascimento"
              [(ngModel)]="nascimento"
              required
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
      Funcionários já cadastrados usam os mesmos dados para abrir o painel administrativo.
    </p>
  </section>`,
})
export class IdentificacaoComponent {
  cpf = '';
  nascimento = '';
  erro = signal('');
  carregando = signal(false);
  constructor(
    private sessao: SessaoService,
    private router: Router,
    private rota: ActivatedRoute,
  ) {}
  entrar() {
    this.erro.set('');
    this.carregando.set(true);
    this.sessao.identificar(Number(this.cpf), this.nascimento).subscribe({
      next: (usuario) => {
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
        this.carregando.set(false);
      },
    });
  }
}
