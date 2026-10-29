using System.ComponentModel.DataAnnotations;

namespace AplicacaoBackEnd.DTO;

public class PedidoStatusDTO
{
    [Required]
    [RegularExpression("^[APFC]$", ErrorMessage = "Use A (Aberto), P (Em processamento), F (Concluído) ou C (Cancelado).")]
    public string StatusAtendimento { get; set; } = string.Empty;
}
