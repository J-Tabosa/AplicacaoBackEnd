using System.ComponentModel.DataAnnotations;

namespace AplicacaoBackEnd.DTO;

public class CategoriaDTO
{
    [Required, StringLength(35)]
    public string Categoria { get; set; } = string.Empty;
}

public class ProdutoDadosDTO
{
    public int CodigoPreco { get; set; }
    [Range(typeof(decimal), "0", "99999999.99", ParseLimitsInInvariantCulture = true)]
    public decimal PrecoVenda { get; set; }
    [Required, StringLength(10)]
    public string Unidade { get; set; } = string.Empty;
}

public class ItemDTO : IValidatableObject
{
    [Range(1, int.MaxValue)]
    public int IdCategoria { get; set; }
    [Required, StringLength(40)]
    public string Item { get; set; } = string.Empty;
    [StringLength(200)]
    public string? DescricaoItem { get; set; }
    [Range(typeof(decimal), "0", "99999999.99", ParseLimitsInInvariantCulture = true)]
    public decimal Preco { get; set; }
    [Required, RegularExpression("^[PS]$")]
    public string TipoItem { get; set; } = "S";
    [Required, RegularExpression("^[SN]$")]
    public string AtivoProdutoServico { get; set; } = "S";
    public ProdutoDadosDTO? Produto { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (TipoItem == "P" && Produto == null)
            yield return new ValidationResult("Informe os dados de produto para itens do tipo P.", new[] { nameof(Produto) });
        if (TipoItem == "S" && Produto != null)
            yield return new ValidationResult("Serviços não possuem dados de produto ou estoque.", new[] { nameof(Produto) });
    }
}
