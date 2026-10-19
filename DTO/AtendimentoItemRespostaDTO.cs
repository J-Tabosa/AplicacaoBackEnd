namespace AplicacaoBackEnd.DTO
{
    // Dados devolvidos no JSON da resposta.
    public class AtendimentoItemRespostaDTO
    {
        public int IdItem { get; set; }

        public string Item { get; set; } = string.Empty;

        public string TipoItem { get; set; } = string.Empty;

        public int IdFuncionario { get; set; }

        public string Funcionario { get; set; } = string.Empty;

        public int QuantidadeItem { get; set; }

        public decimal? ValorUnitarioEntrada { get; set; }

        public decimal Subtotal { get; set; }
    }
}
