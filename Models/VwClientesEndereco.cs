using System;
using System.Collections.Generic;

namespace AplicacaoBackEnd.Models;

public partial class VwClientesEndereco
{
    public int Idpessoa { get; set; }

    public string Nome { get; set; } = null!;

    public long Cpf { get; set; }

    public string? Email { get; set; }

    public long? Telefone { get; set; }

    public string Cep { get; set; } = null!;

    public string? Numero { get; set; }

    public string? Complemento { get; set; }
}
