import { dataHoje } from './data';
import { Pessoa } from './pessoa';

export class AtendimentoItem {
  idItem = 0;
  item = '';
  tipoItem = '';
  idFuncionario = 0;
  funcionario = '';
  quantidadeItem = 1;
  valorUnitarioEntrada = 0;
  subtotal = 0;
}

export class Atendimento {
  idAtendimento = 0;
  idPessoa = 0;
  cliente = '';
  pessoa = new Pessoa();
  dataAtendimento = dataHoje();
  statusAtendimento: string | null = 'A';
  itens: AtendimentoItem[] = [new AtendimentoItem()];
  valorTotal = 0;
}
