namespace AplicacaoBackEnd.DTO
{
    // Dados devolvidos no JSON da resposta.
    public class EntradaProdutoRespostaDTO
    {
        public int IdEntradaProduto { get; set; }

        public int IdItem { get; set; }

        public string Produto { get; set; } = string.Empty;

        public DateOnly DataEntradaProduto { get; set; }

        public decimal QuantidadeEntrada { get; set; }

        public string? Unidade { get; set; }

        public decimal ValorUnitarioEntrada { get; set; }

        public int QuantidadeEstoque { get; set; }

        public decimal? PrecoVenda { get; set; }
    }
}
