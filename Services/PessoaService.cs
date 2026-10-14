using AplicacaoBackEnd.DTO;
using AplicacaoBackEnd.Models;
using Microsoft.EntityFrameworkCore;

namespace AplicacaoBackEnd.Services;

public class PessoaService
{
    private readonly DbAtendimentoContext _context;
    public PessoaService(DbAtendimentoContext context) => _context = context;

    public IQueryable<Pessoa> Consulta() => _context.Pessoas.AsNoTracking().Include(p => p.Enderecos);

    public async Task<Pessoa> BloquearAsync(int id)
    {
        var registros = await _context.Pessoas
            .FromSqlInterpolated($"SELECT * FROM pessoa WHERE idpessoa = {id} FOR UPDATE").ToListAsync();
        var pessoa = registros.SingleOrDefault() ?? throw new RegraNegocioException(404, "Pessoa não localizada.");
        await _context.Entry(pessoa).Collection(p => p.Enderecos).LoadAsync();
        return pessoa;
    }

    private async Task ValidarAsync(PessoaEnderecoDTO dados, int id = 0)
    {
        Validacao.Data(dados.DataNascimento, "nascimento");
        if (dados.Enderecos == null || dados.Enderecos.Count == 0 || dados.Enderecos.Any(e => e == null))
            throw new RegraNegocioException(400, "Informe pelo menos um endereço válido.");
        if (await _context.Pessoas.AnyAsync(p => p.Cpf == dados.Cpf && p.Idpessoa != id))
            throw new RegraNegocioException(409, "Já existe outra pessoa com este CPF.");
    }

    private static void AplicarPessoa(Pessoa pessoa, PessoaEnderecoDTO dados)
    {
        pessoa.Nome = dados.Nome;
        pessoa.Cpf = dados.Cpf;
        pessoa.DataNascimento = dados.DataNascimento!.Value;
        pessoa.Sexo = dados.Sexo;
        pessoa.Email = dados.Email;
        pessoa.Telefone = dados.Telefone;
        pessoa.AtivoPessoa = dados.AtivoPessoa;
    }

    private static void AplicarEndereco(Endereco endereco, EnderecoDTO dados)
    {
        endereco.Cep = dados.Cep;
        endereco.Numero = dados.Numero;
        endereco.Complemento = dados.Complemento;
    }

    // Chamado dentro da transação do controller de pessoa ou funcionário.
    public async Task<Pessoa> CriarAsync(PessoaEnderecoDTO dados)
    {
        await ValidarAsync(dados);
        if (dados.Enderecos.Any(e => e.Idendereco != 0))
            throw new RegraNegocioException(400, "Endereços novos devem ser enviados sem ID ou com ID zero.");
        var pessoa = new Pessoa();
        AplicarPessoa(pessoa, dados);
        foreach (var entrada in dados.Enderecos)
        {
            var endereco = new Endereco();
            AplicarEndereco(endereco, entrada);
            pessoa.Enderecos.Add(endereco);
        }
        _context.Pessoas.Add(pessoa);
        await _context.SaveChangesAsync();
        return pessoa;
    }

    public async Task AtualizarAsync(Pessoa pessoa, PessoaEnderecoDTO dados)
    {
        await ValidarAsync(dados, pessoa.Idpessoa);
        var ids = dados.Enderecos.Where(e => e.Idendereco > 0).Select(e => e.Idendereco).ToList();
        if (ids.Count != ids.Distinct().Count())
            throw new RegraNegocioException(400, "Não repita o ID de um endereço.");
        var existentes = pessoa.Enderecos.ToDictionary(e => e.Idendereco);
        if (ids.Any(id => !existentes.ContainsKey(id)))
            throw new RegraNegocioException(400, "Um dos endereços não existe ou pertence a outra pessoa.");
        AplicarPessoa(pessoa, dados);
        _context.Enderecos.RemoveRange(pessoa.Enderecos.Where(e => !ids.Contains(e.Idendereco)).ToList());
        foreach (var entrada in dados.Enderecos)
        {
            if (entrada.Idendereco > 0)
                AplicarEndereco(existentes[entrada.Idendereco], entrada);
            else
            {
                var endereco = new Endereco { Idpessoa = pessoa.Idpessoa, IdpessoaNavigation = pessoa };
                AplicarEndereco(endereco, entrada);
                _context.Enderecos.Add(endereco);
            }
        }
        await _context.SaveChangesAsync();
    }
}
