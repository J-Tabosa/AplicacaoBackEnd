using System;
using System.Collections.Generic;

namespace AplicacaoBackEnd.Models;

public partial class Estoque
{
    public int Iditem { get; set; }

    public int QuantidadeEstoque { get; set; }

    public virtual Produto IditemNavigation { get; set; } = null!;
}
