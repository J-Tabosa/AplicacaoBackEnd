import { dataHoje } from './data';

export class Entrada {
  idEntradaProduto = 0;
  idItem = 0;
  produto = '';
  dataEntradaProduto = dataHoje();
  quantidadeEntrada = 1;
  unidade: string | null = 'UN';
  valorUnitarioEntrada = 0;
  quantidadeEstoque = 0;
  precoVenda: number | null = 0;
}
