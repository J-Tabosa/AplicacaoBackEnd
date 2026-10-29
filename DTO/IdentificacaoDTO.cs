using System.ComponentModel.DataAnnotations;

namespace AplicacaoBackEnd.DTO;

public class IdentificacaoDTO
{
    [Range(1.0, 99999999999.0)]
    public long Cpf { get; set; }

    [Required]
    public DateOnly? DataNascimento { get; set; }
}
