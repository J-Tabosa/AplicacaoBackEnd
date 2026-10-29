using System.ComponentModel.DataAnnotations;

namespace AplicacaoBackEnd.DTO;

public class CompraDTO
{
    [Required]
    [MinLength(1)]
    [MaxLength(100)]
    public List<CompraItemDTO> Itens { get; set; } = new List<CompraItemDTO>();
}

public class CompraItemDTO
{
    [Range(1, int.MaxValue)]
    public int IdItem { get; set; }

    [Range(1, int.MaxValue)]
    public int QuantidadeItem { get; set; }
}
