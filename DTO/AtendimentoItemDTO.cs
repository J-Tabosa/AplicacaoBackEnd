using System.ComponentModel.DataAnnotations;

namespace AplicacaoBackEnd.DTO
{
    // Dados enviados no JSON da requisição.
    public class AtendimentoItemDTO
    {
        [Range(1, int.MaxValue)]
        public int IdItem { get; set; }

        [Range(1, int.MaxValue)]
        public int IdFuncionario { get; set; }

        [Range(1, int.MaxValue)]
        public int QuantidadeItem { get; set; }

        [Required]
        [Range(typeof(decimal), "0", "99999999.99", ParseLimitsInInvariantCulture = true)]
        public decimal? ValorUnitarioEntrada { get; set; }
    }
}
