using System.ComponentModel.DataAnnotations;

namespace AplicacaoBackEnd.DTO;

public class ProdutoDTO
{
    // Dados da tabela item.
    [Range(1, int.MaxValue)]
    public int IdCategoria { get; set; }

    [Required, StringLength(40)]
    public string Item { get; set; } = string.Empty;

    [StringLength(200)]
    public string? DescricaoItem { get; set; }

    [Range(typeof(decimal), "0", "99999999.99")]
    public decimal Preco { get; set; }

    [Required, RegularExpression("^P$", ErrorMessage = "O tipo do produto deve ser P.")]
    public string TipoItem { get; set; } = "P";

    [Required, RegularExpression("^[SN]$", ErrorMessage = "Informe S ou N para o estado do produto.")]
    public string AtivoProdutoServico { get; set; } = "S";

    // Dados da tabela produto.
    public int CodigoPreco { get; set; }

    [Range(typeof(decimal), "0", "99999999.99")]
    public decimal PrecoVenda { get; set; }

    [Required, StringLength(10)]
    public string Unidade { get; set; } = string.Empty;
}
