using System.ComponentModel.DataAnnotations;

namespace AplicacaoBackEnd.DTO
{
    // Dados enviados no JSON da requisição.
    public class FuncionarioCadastroDTO
    {
        // Dados pessoais.
        [Required]
        [StringLength(100)]
        public string Nome { get; set; } = string.Empty;

        [Range(1.0, 99999999999.0)]
        public long Cpf { get; set; }

        [Required]
        public DateOnly? DataNascimento { get; set; }

        [StringLength(1)]
        public string? Sexo { get; set; }

        [EmailAddress]
        [StringLength(100)]
        public string? Email { get; set; }

        public long? Telefone { get; set; }

        [Required]
        [RegularExpression("^[SN]$")]
        public string AtivoPessoa { get; set; } = "S";

        // Envie a lista explicitamente. No PUT ela representa a lista final de endereços.
        [Required]
        [MinLength(1)]
        public List<EnderecoDTO> Enderecos { get; set; } = new List<EnderecoDTO>();

        // Dados profissionais.
        [Required]
        [StringLength(20)]
        public string Cargo { get; set; } = string.Empty;

        [Required]
        public DateOnly? DataAdmissao { get; set; }
    }
}
