using System;
using System.Collections.Generic;

namespace AplicacaoBackEnd.Models;

public partial class VwAtendimento
{
    public int Idatendimento { get; set; }

    public DateOnly DataAtendimento { get; set; }

    public string? StatusAtendimento { get; set; }

    public int Idcliente { get; set; }

    public string Cliente { get; set; } = null!;

    public int Iditem { get; set; }

    public string Item { get; set; } = null!;

    public string TipoItem { get; set; } = null!;

    public int QuantidadeItem { get; set; }

    public decimal? ValorunitarioEntrada { get; set; }

    public int Idfuncionario { get; set; }

    public string Funcionario { get; set; } = null!;
}
