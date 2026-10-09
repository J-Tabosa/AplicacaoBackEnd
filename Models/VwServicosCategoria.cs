using System;
using System.Collections.Generic;

namespace AplicacaoBackEnd.Models;

public partial class VwServicosCategoria
{
    public int Iditem { get; set; }

    public string Item { get; set; } = null!;

    public string? DescricaoItem { get; set; }

    public decimal Preco { get; set; }

    public string Categoria { get; set; } = null!;
}
