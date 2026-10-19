namespace AplicacaoBackEnd.DTO
{
    // Dados devolvidos no JSON da resposta.
    public class ItemRespostaDTO
    {
        public int IdItem { get; set; }

        public int IdCategoria { get; set; }

        public string Categoria { get; set; } = string.Empty;

        public string Item { get; set; } = string.Empty;

        public string? DescricaoItem { get; set; }

        public decimal Preco { get; set; }

        public string TipoItem { get; set; } = string.Empty;

        public string? AtivoProdutoServico { get; set; }

        public ProdutoResumoDTO? Produto { get; set; }
    }
}
