using System;
using System.Collections.Generic;

namespace AplicacaoBackEnd.Models;

public partial class Produto
{
    public int Iditem { get; set; }

    public int? CodigoPreco { get; set; }

    public decimal? PrecoVenda { get; set; }

    public string? Unidade { get; set; }

    public virtual ICollection<EntradaProduto> EntradaProdutos { get; set; } = new List<EntradaProduto>();

    public virtual Estoque? Estoque { get; set; }

    public virtual Item IditemNavigation { get; set; } = null!;
}
