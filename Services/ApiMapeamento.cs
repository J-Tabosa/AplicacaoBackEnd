using AplicacaoBackEnd.DTO;
using AplicacaoBackEnd.Models;

namespace AplicacaoBackEnd.Services;

public static class ApiMapeamento
{
    private static void DadosPessoa(Pessoa origem, PessoaRespostaDTO destino)
    {
        destino.Idpessoa = origem.Idpessoa;
        destino.Nome = origem.Nome;
        destino.Cpf = origem.Cpf;
        destino.DataNascimento = origem.DataNascimento;
        destino.Sexo = origem.Sexo;
        destino.Email = origem.Email;
        destino.Telefone = origem.Telefone;
        destino.AtivoPessoa = origem.AtivoPessoa;
        destino.Enderecos = origem.Enderecos.OrderBy(e => e.Idendereco).Select(e => new EnderecoRespostaDTO
        {
            Idendereco = e.Idendereco, Cep = e.Cep, Numero = e.Numero, Complemento = e.Complemento
        }).ToList();
    }

    public static PessoaRespostaDTO Pessoa(Pessoa pessoa)
    {
        var resposta = new PessoaRespostaDTO();
        DadosPessoa(pessoa, resposta);
        return resposta;
    }

    public static FuncionarioRespostaDTO Funcionario(Funcionario funcionario)
    {
        var resposta = new FuncionarioRespostaDTO { Cargo = funcionario.Cargo, DataAdmissao = funcionario.DataAdmissao };
        DadosPessoa(funcionario.IdpessoaNavigation, resposta);
        return resposta;
    }

    public static ItemRespostaDTO Item(Item item) => new()
    {
        IdItem = item.Iditem, IdCategoria = item.Idcategoria,
        Categoria = item.IdcategoriaNavigation.Categoria,
        Item = item.Item1, DescricaoItem = item.DescricaoItem, Preco = item.Preco,
        TipoItem = item.TipoItem, AtivoProdutoServico = item.AtivoProdutoServico,
        Produto = item.Produto == null ? null : new ProdutoResumoDTO
        {
            CodigoPreco = item.Produto.CodigoPreco, PrecoVenda = item.Produto.PrecoVenda,
            Unidade = item.Produto.Unidade, QuantidadeEstoque = item.Produto.Estoque?.QuantidadeEstoque
        }
    };

    public static ProdutoRespostaDTO Produto(Produto produto)
    {
        var item = produto.IditemNavigation;
        return new ProdutoRespostaDTO
        {
            IdItem = produto.Iditem, IdCategoria = item.Idcategoria, Item = item.Item1,
            DescricaoItem = item.DescricaoItem, Preco = item.Preco, TipoItem = item.TipoItem,
            AtivoProdutoServico = item.AtivoProdutoServico, CodigoPreco = produto.CodigoPreco,
            PrecoVenda = produto.PrecoVenda, Unidade = produto.Unidade
        };
    }

    public static EntradaProdutoRespostaDTO Entrada(EntradaProduto entrada) => new()
    {
        IdEntradaProduto = entrada.IdentradaProduto, IdItem = entrada.Iditem,
        Produto = entrada.IditemNavigation.IditemNavigation.Item1,
        DataEntradaProduto = entrada.DataEntradaProduto, QuantidadeEntrada = entrada.QuantidadeEntrada,
        Unidade = entrada.Unidade, ValorUnitarioEntrada = entrada.ValorUnitarioEntrada,
        QuantidadeEstoque = entrada.IditemNavigation.Estoque?.QuantidadeEstoque ?? 0,
        PrecoVenda = entrada.IditemNavigation.PrecoVenda
    };

    public static AtendimentoRespostaDTO Atendimento(Atendimento atendimento) => new()
    {
        IdAtendimento = atendimento.Idatendimento, IdPessoa = atendimento.Idpessoa,
        Cliente = atendimento.IdpessoaNavigation.Nome, DataAtendimento = atendimento.DataAtendimento,
        Pessoa = ApiMapeamento.Pessoa(atendimento.IdpessoaNavigation),
        StatusAtendimento = atendimento.StatusAtendimento,
        Itens = atendimento.Atendimentoitems.OrderBy(i => i.Iditem).Select(i => new AtendimentoItemRespostaDTO
        {
            IdItem = i.Iditem, Item = i.IditemNavigation.Item1, TipoItem = i.IditemNavigation.TipoItem,
            IdFuncionario = i.Idfuncionario, Funcionario = i.IdfuncionarioNavigation.IdpessoaNavigation.Nome,
            QuantidadeItem = i.QuantidadeItem, ValorUnitarioEntrada = i.ValorunitarioEntrada,
            Subtotal = i.QuantidadeItem * (i.ValorunitarioEntrada ?? 0)
        }).ToList()
    };
}
