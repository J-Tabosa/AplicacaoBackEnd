using System;
using System.Collections.Generic;

namespace AplicacaoBackEnd.Models;

public partial class Atendimentoitem
{
    public int Idatendimento { get; set; }

    public int Iditem { get; set; }

    public int Idfuncionario { get; set; }

    public int QuantidadeItem { get; set; }

    public decimal? ValorunitarioEntrada { get; set; }

    public virtual Atendimento IdatendimentoNavigation { get; set; } = null!;

    public virtual Funcionario IdfuncionarioNavigation { get; set; } = null!;

    public virtual Item IditemNavigation { get; set; } = null!;
}
