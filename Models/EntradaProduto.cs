using System;
using System.Collections.Generic;

namespace AplicacaoBackEnd.Models;

public partial class EntradaProduto
{
    public int IdentradaProduto { get; set; }

    public int Iditem { get; set; }

    public DateOnly DataEntradaProduto { get; set; }

    public decimal QuantidadeEntrada { get; set; }

    public string? Unidade { get; set; }

    public decimal ValorUnitarioEntrada { get; set; }

    public virtual Produto IditemNavigation { get; set; } = null!;
}
