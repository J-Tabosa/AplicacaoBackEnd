using System.ComponentModel.DataAnnotations;

namespace AplicacaoBackEnd.DTO;

public class EntradaProdutoDTO : IValidatableObject
{
    [Range(1, int.MaxValue)]
    public int IdItem { get; set; }
    [Required]
    public DateOnly? DataEntradaProduto { get; set; }
    [Range(typeof(decimal), "1", "99999999", ParseLimitsInInvariantCulture = true)]
    public decimal QuantidadeEntrada { get; set; }
    [Required, StringLength(10)]
    public string Unidade { get; set; } = string.Empty;
    [Range(typeof(decimal), "0", "99999999.99", ParseLimitsInInvariantCulture = true)]
    public decimal ValorUnitarioEntrada { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // O esquema fornecido usa INT no saldo de estoque.
        if (QuantidadeEntrada != decimal.Truncate(QuantidadeEntrada))
            yield return new ValidationResult("O estoque deste banco trabalha com unidades inteiras.", new[] { nameof(QuantidadeEntrada) });
    }
}

public class AtendimentoItemDTO
{
    [Range(1, int.MaxValue)]
    public int IdItem { get; set; }
    [Range(1, int.MaxValue)]
    public int IdFuncionario { get; set; }
    [Range(1, int.MaxValue)]
    public int QuantidadeItem { get; set; }
    [Required, Range(typeof(decimal), "0", "99999999.99", ParseLimitsInInvariantCulture = true)]
    public decimal? ValorUnitarioEntrada { get; set; }
}

public class AtendimentoDTO
{
    [Range(1, int.MaxValue)]
    public int IdPessoa { get; set; }
    [Required]
    public DateOnly? DataAtendimento { get; set; }
    [Required, StringLength(1, MinimumLength = 1)]
    public string StatusAtendimento { get; set; } = "A";
    [Required, MinLength(1)]
    public List<AtendimentoItemDTO> Itens { get; set; } = null!;
}
