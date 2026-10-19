namespace AplicacaoBackEnd.DTO
{
    // Dados devolvidos no JSON da resposta.
    public class ProdutoRespostaDTO
    {
        public int IdItem { get; set; }

        public int IdCategoria { get; set; }

        public string Item { get; set; } = string.Empty;

        public string? DescricaoItem { get; set; }

        public decimal Preco { get; set; }

        public string TipoItem { get; set; } = string.Empty;

        public string? AtivoProdutoServico { get; set; }

        public int? CodigoPreco { get; set; }

        public decimal? PrecoVenda { get; set; }

        public string? Unidade { get; set; }
    }
}
