using System.ComponentModel.DataAnnotations;

namespace AplicacaoBackEnd.DTO
{
    // Dados enviados no JSON da requisição.
    public class EntradaProdutoDTO
    {
        [Range(1, int.MaxValue)]
        public int IdItem { get; set; }

        [Required]
        public DateOnly? DataEntradaProduto { get; set; }

        [Range(1.0, 99999999.0)]
        public decimal QuantidadeEntrada { get; set; }

        [Required]
        [StringLength(10)]
        public string Unidade { get; set; } = string.Empty;

        [Range(0.0, 99999999.99)]
        public decimal ValorUnitarioEntrada { get; set; }
    }
}
