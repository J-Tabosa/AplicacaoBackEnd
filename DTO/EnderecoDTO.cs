using System.ComponentModel.DataAnnotations;

namespace AplicacaoBackEnd.DTO;

public class EnderecoDTO
{
    // No POST e para novos endereços no PUT, informe 0 ou omita o ID.
    [Range(0, int.MaxValue)]
    public int Idendereco { get; set; }

    [Required, RegularExpression("^[0-9]{8}$", ErrorMessage = "Informe o CEP com 8 dígitos, sem hífen.")]
    public string Cep { get; set; } = string.Empty;

    [StringLength(10)]
    public string? Numero { get; set; }

    [StringLength(150)]
    public string? Complemento { get; set; }
}
