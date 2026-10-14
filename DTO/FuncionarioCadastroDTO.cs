using System.ComponentModel.DataAnnotations;

namespace AplicacaoBackEnd.DTO;

// Reaproveita os dados pessoais, os endereços e suas validações.
public class FuncionarioCadastroDTO : PessoaEnderecoDTO
{
    [Required, StringLength(20)]
    public string Cargo { get; set; } = string.Empty;

    [Required]
    public DateOnly? DataAdmissao { get; set; }
}
