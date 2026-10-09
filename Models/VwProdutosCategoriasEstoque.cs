using System;
using System.Collections.Generic;

namespace AplicacaoBackEnd.Models;

public partial class VwProdutosCategoriasEstoque
{
    public int Iditem { get; set; }

    public string Item { get; set; } = null!;

    public string? DescricaoItem { get; set; }

    public decimal Preco { get; set; }

    public string Categoria { get; set; } = null!;

    public int? CodigoPreco { get; set; }

    public decimal? PrecoVenda { get; set; }

    public string? Unidade { get; set; }

    public int QuantidadeEstoque { get; set; }
}
