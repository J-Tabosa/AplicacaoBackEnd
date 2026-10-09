using System;
using System.Collections.Generic;

namespace AplicacaoBackEnd.Models;

public partial class Endereco
{
    public int Idendereco { get; set; }

    public int Idpessoa { get; set; }

    public string Cep { get; set; } = null!;

    public string? Numero { get; set; }

    public string? Complemento { get; set; }

    public virtual Pessoa? IdpessoaNavigation { get; set; }
}
