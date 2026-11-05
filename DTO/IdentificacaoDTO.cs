using System.ComponentModel.DataAnnotations;

namespace AplicacaoBackEnd.DTO;

public class IdentificacaoDTO
{
    [Required, StringLength(254)]
    public string Login { get; set; } = string.Empty;

    [Required, StringLength(128)]
    public string Senha { get; set; } = string.Empty;
}
