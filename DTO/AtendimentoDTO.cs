using System.ComponentModel.DataAnnotations;

namespace AplicacaoBackEnd.DTO
{
    // Dados enviados no JSON da requisição.
    public class AtendimentoDTO
    {
        [Range(1, int.MaxValue)]
        public int IdPessoa { get; set; }

        [Required]
        public DateOnly? DataAtendimento { get; set; }

        [Required]
        [StringLength(1, MinimumLength = 1)]
        public string StatusAtendimento { get; set; } = "A";

        [Required]
        [MinLength(1)]
        public List<AtendimentoItemDTO> Itens { get; set; } = new List<AtendimentoItemDTO>();
    }
}
