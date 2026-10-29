export interface Estoque {
  idItem: number;
  item: string;
  descricaoItem: string | null;
  preco: number;
  precoVenda: number | null;
  unidade: string | null;
  categoria: string;
  quantidadeEstoque: number;
  ativoProdutoServico: string | null;
}
