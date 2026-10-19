using System.ComponentModel.DataAnnotations;

namespace AplicacaoBackEnd.DTO
{
    // Dados enviados no JSON da requisição.
    public class CategoriaDTO
    {
        [Required]
        [StringLength(35)]
        public string Categoria { get; set; } = string.Empty;
    }
}
