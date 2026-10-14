using System.ComponentModel.DataAnnotations;

namespace AplicacaoBackEnd.DTO;

public class FuncionarioDTO
{
    // O funcionário usa o ID de uma pessoa já cadastrada.
    [Range(1, int.MaxValue)]
    public int IdPessoa { get; set; }

    [Required, StringLength(20)]
    public string Cargo { get; set; } = string.Empty;

    [Required]
    public DateOnly? DataAdmissao { get; set; }
}
