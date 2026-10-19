using System.ComponentModel.DataAnnotations;

namespace AplicacaoBackEnd.DTO
{
    // Dados enviados no JSON da requisição.
    public class ItemDTO
    {
        [Range(1, int.MaxValue)]
        public int IdCategoria { get; set; }

        [Required]
        [StringLength(40)]
        public string Item { get; set; } = string.Empty;

        [StringLength(200)]
        public string? DescricaoItem { get; set; }

        [Range(0.0, 99999999.99)]
        public decimal Preco { get; set; }

        [Required]
        [RegularExpression("^[PS]$")]
        public string TipoItem { get; set; } = "S";

        [Required]
        [RegularExpression("^[SN]$")]
        public string AtivoProdutoServico { get; set; } = "S";

        public ProdutoDadosDTO? Produto { get; set; }
    }
}
