export class Produto {
  codigoPreco = 0;
  precoVenda: number | null = 0;
  unidade: string | null = 'UN';
  quantidadeEstoque: number | null = 0;
}

export class Item {
  idItem = 0;
  idCategoria = 0;
  categoria = '';
  item = '';
  descricaoItem: string | null = '';
  preco = 0;
  tipoItem = 'S';
  ativoProdutoServico: string | null = 'S';
  produto: Produto | null = null;
}
