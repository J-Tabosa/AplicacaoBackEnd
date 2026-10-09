using System;
using System.Collections.Generic;

namespace AplicacaoBackEnd.Models;

public partial class Item
{
    public int Iditem { get; set; }

    public int Idcategoria { get; set; }

    public string Item1 { get; set; } = null!;

    public string? DescricaoItem { get; set; }

    public decimal Preco { get; set; }

    public string TipoItem { get; set; } = null!;

    public string? AtivoProdutoServico { get; set; }

    public virtual ICollection<Atendimentoitem> Atendimentoitems { get; set; } = new List<Atendimentoitem>();

    public virtual Categorium IdcategoriaNavigation { get; set; } = null!;

    public virtual Produto? Produto { get; set; }
}
