namespace AplicacaoBackEnd.DTO;

public class EnderecoRespostaDTO
{
    public int Idendereco { get; set; }
    public string Cep { get; set; } = string.Empty;
    public string? Numero { get; set; }
    public string? Complemento { get; set; }
}

public class PessoaRespostaDTO
{
    public int Idpessoa { get; set; }
    public string Nome { get; set; } = string.Empty;
    public long Cpf { get; set; }
    public DateOnly DataNascimento { get; set; }
    public string? Sexo { get; set; }
    public string? Email { get; set; }
    public long? Telefone { get; set; }
    public string? AtivoPessoa { get; set; }
    public List<EnderecoRespostaDTO> Enderecos { get; set; } = new();
}

public class FuncionarioRespostaDTO : PessoaRespostaDTO
{
    public string Cargo { get; set; } = string.Empty;
    public DateOnly DataAdmissao { get; set; }
}

public class CategoriaRespostaDTO
{
    public int Idcategoria { get; set; }
    public string Categoria { get; set; } = string.Empty;
}

public class ProdutoResumoDTO
{
    public int? CodigoPreco { get; set; }
    public decimal? PrecoVenda { get; set; }
    public string? Unidade { get; set; }
    public int? QuantidadeEstoque { get; set; }
}

public class ItemRespostaDTO
{
    public int IdItem { get; set; }
    public int IdCategoria { get; set; }
    public string Categoria { get; set; } = string.Empty;
    public string Item { get; set; } = string.Empty;
    public string? DescricaoItem { get; set; }
    public decimal Preco { get; set; }
    public string TipoItem { get; set; } = string.Empty;
    public string? AtivoProdutoServico { get; set; }
    public ProdutoResumoDTO? Produto { get; set; }
}

public class ProdutoRespostaDTO
{
    public int IdItem { get; set; }
    public int IdCategoria { get; set; }
    public string Item { get; set; } = string.Empty;
    public string? DescricaoItem { get; set; }
    public decimal Preco { get; set; }
    public string TipoItem { get; set; } = string.Empty;
    public string? AtivoProdutoServico { get; set; }
    public int? CodigoPreco { get; set; }
    public decimal? PrecoVenda { get; set; }
    public string? Unidade { get; set; }
}

public class EntradaProdutoRespostaDTO
{
    public int IdEntradaProduto { get; set; }
    public int IdItem { get; set; }
    public string Produto { get; set; } = string.Empty;
    public DateOnly DataEntradaProduto { get; set; }
    public decimal QuantidadeEntrada { get; set; }
    public string? Unidade { get; set; }
    public decimal ValorUnitarioEntrada { get; set; }
    public int QuantidadeEstoque { get; set; }
    public decimal? PrecoVenda { get; set; }
}

public class AtendimentoItemRespostaDTO
{
    public int IdItem { get; set; }
    public string Item { get; set; } = string.Empty;
    public string TipoItem { get; set; } = string.Empty;
    public int IdFuncionario { get; set; }
    public string Funcionario { get; set; } = string.Empty;
    public int QuantidadeItem { get; set; }
    public decimal? ValorUnitarioEntrada { get; set; }
    public decimal Subtotal { get; set; }
}

public class AtendimentoRespostaDTO
{
    public int IdAtendimento { get; set; }
    public int IdPessoa { get; set; }
    public string Cliente { get; set; } = string.Empty;
    public PessoaRespostaDTO Pessoa { get; set; } = new();
    public DateOnly DataAtendimento { get; set; }
    public string? StatusAtendimento { get; set; }
    public List<AtendimentoItemRespostaDTO> Itens { get; set; } = new();
    public decimal ValorTotal => Itens.Sum(i => i.Subtotal);
}
