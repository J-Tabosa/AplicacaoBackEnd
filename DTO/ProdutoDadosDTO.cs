using System.ComponentModel.DataAnnotations;

namespace AplicacaoBackEnd.DTO
{
    // Dados enviados no JSON da requisição.
    public class ProdutoDadosDTO
    {
        public int CodigoPreco { get; set; }

        [Range(0.0, 99999999.99)]
        public decimal PrecoVenda { get; set; }

        [Required]
        [StringLength(10)]
        public string Unidade { get; set; } = string.Empty;
    }
}
