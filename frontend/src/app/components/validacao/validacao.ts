import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { NgForm } from '@angular/forms';

@Component({
  selector: 'app-validacao',
  changeDetection: ChangeDetectionStrategy.Default,
  template: `@if (mensagens().length) {
    <div class="mensagem erro" role="alert">
      <strong>Confira os campos:</strong>
      <ul>
        @for (mensagem of mensagens(); track mensagem) {
          <li>{{ mensagem }}</li>
        }
      </ul>
    </div>
  }`,
})
export class ValidacaoComponent {
  @Input({ required: true }) form!: NgForm;
  mensagens(): string[] {
    if (!this.form) return [];
    const textos: string[] = [];
    const nomes: Record<string, string> = {
      nome: 'Nome',
      cpf: 'CPF',
      nascimento: 'Data de nascimento',
      email: 'E-mail',
      telefone: 'Telefone',
      admissao: 'Data de admissão',
      cargo: 'Cargo',
      categoria: 'Categoria',
      item: 'Item',
      preco: 'Preço',
      venda: 'Preço de venda',
      quantidade: 'Quantidade',
      valor: 'Custo unitário',
      data: 'Data',
      cliente: 'Cliente',
    };
    for (const [nome, campo] of Object.entries(this.form.controls)) {
      if (!campo.invalid || (!campo.touched && !this.form.submitted)) continue;
      let rotulo = nomes[nome] || nome;
      if (nome.startsWith('cep')) rotulo = 'CEP do endereço ' + (Number(nome.slice(3)) + 1);
      if (campo.hasError('required')) textos.push(`${rotulo}: campo obrigatório.`);
      else if (campo.hasError('email')) textos.push(`${rotulo}: informe um e-mail válido.`);
      else if (campo.hasError('pattern'))
        textos.push(`${rotulo}: confira o formato e a quantidade de dígitos.`);
      else if (campo.hasError('min') || campo.hasError('max'))
        textos.push(`${rotulo}: valor fora do intervalo permitido.`);
      else textos.push(`${rotulo}: confira o valor informado.`);
    }
    return textos;
  }
}
