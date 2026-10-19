namespace AplicacaoBackEnd.DTO
{
    // Dados devolvidos no JSON da resposta.
    public class ProdutoResumoDTO
    {
        public int? CodigoPreco { get; set; }

        public decimal? PrecoVenda { get; set; }

        public string? Unidade { get; set; }

        public int? QuantidadeEstoque { get; set; }
    }
}
