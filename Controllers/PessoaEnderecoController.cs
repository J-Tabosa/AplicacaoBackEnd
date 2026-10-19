using AplicacaoBackEnd.DTO;
using AplicacaoBackEnd.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace AplicacaoBackEnd.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PessoaEnderecoController : ControllerBase
{
    private readonly DbAtendimentoContext _context;
    private readonly ILogger<PessoaEnderecoController> _logger;

    public PessoaEnderecoController(DbAtendimentoContext context, ILogger<PessoaEnderecoController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/PessoaEndereco/all
    [HttpGet("all")]
    public async Task<IActionResult> GetPessoasEnderecos()
    {
        var registros = await _context.Pessoas
            .Include(p => p.Enderecos)
            .AsNoTracking()
            .OrderBy(r => r.Idpessoa)
            .ToListAsync();

        var resposta = new List<object>();
        foreach (var registro in registros)
        {
            resposta.Add(ExibirPessoa(registro));
        }

        return Ok(resposta);
    }

    // GET: api/PessoaEndereco/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetPessoaEndereco(int id)
    {
        var pessoa = await _context.Pessoas
            .Include(p => p.Enderecos)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Idpessoa == id);

        if (pessoa == null)
        {
            return NotFound(new { mensagem = "Registro não localizado." });
        }

        return Ok(ExibirPessoa(pessoa));
    }

    // POST: api/PessoaEndereco
    [HttpPost]
    public async Task<IActionResult> PostPessoaEndereco(PessoaEnderecoDTO dados)
    {
        if (dados.DataNascimento == null || dados.DataNascimento.Value.Year < 1000)
        {
            return BadRequest(new { mensagem = "Informe uma data de nascimento válida." });
        }

        if (dados.Enderecos == null || dados.Enderecos.Count == 0 || dados.Enderecos.Any(e => e == null))
        {
            return BadRequest(new { mensagem = "Informe pelo menos um endereço válido." });
        }

        if (dados.Enderecos.Any(e => e.Idendereco != 0))
        {
            return BadRequest(new { mensagem = "Endereços novos devem ser enviados sem ID ou com ID zero." });
        }

        if (await _context.Pessoas.AnyAsync(p => p.Cpf == dados.Cpf))
        {
            return Conflict(new { mensagem = "Já existe uma pessoa com este CPF." });
        }

        // Ou grava todas as tabelas, ou desfaz tudo ao sair sem CommitAsync.
        await using var transacao = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            var pessoa = new Pessoa();
            pessoa.Nome = dados.Nome;
            pessoa.Cpf = dados.Cpf;
            pessoa.DataNascimento = dados.DataNascimento.GetValueOrDefault();
            pessoa.Sexo = dados.Sexo;
            pessoa.Email = dados.Email;
            pessoa.Telefone = dados.Telefone;
            pessoa.AtivoPessoa = dados.AtivoPessoa;

            foreach (var endereco in dados.Enderecos)
            {
                pessoa.Enderecos.Add(new Endereco
                {
                    Cep = endereco.Cep,
                    Numero = endereco.Numero,
                    Complemento = endereco.Complemento
                });
            }

            _context.Pessoas.Add(pessoa);
            await _context.SaveChangesAsync();

            await transacao.CommitAsync();
            return CreatedAtAction(nameof(GetPessoaEndereco), new { id = pessoa.Idpessoa }, ExibirPessoa(pessoa));
        }
        catch (DbUpdateException ex)
        {
            await transacao.RollbackAsync();
            _logger.LogError(ex, "Erro ao gravar os dados no banco.");
            return Conflict(new { mensagem = "Não foi possível gravar. Confira os vínculos e se os dados já foram cadastrados." });
        }
    }

    // PUT: api/PessoaEndereco/5
    // A lista de endereços recebida será a lista final desse cadastro.
    [HttpPut("{id:int}")]
    public async Task<IActionResult> PutPessoaEndereco(int id, PessoaEnderecoDTO dados)
    {
        if (dados.DataNascimento == null || dados.DataNascimento.Value.Year < 1000)
        {
            return BadRequest(new { mensagem = "Informe uma data de nascimento válida." });
        }

        if (dados.Enderecos == null || dados.Enderecos.Count == 0 || dados.Enderecos.Any(e => e == null))
        {
            return BadRequest(new { mensagem = "Informe pelo menos um endereço válido." });
        }

        await using var transacao = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            // FOR UPDATE impede duas alterações simultâneas nesta pessoa.
            var pessoas = await _context.Pessoas
                .FromSqlInterpolated($"SELECT * FROM pessoa WHERE idpessoa = {id} FOR UPDATE")
                .ToListAsync();
            var pessoa = pessoas.FirstOrDefault();

            if (pessoa == null)
            {
                return NotFound(new { mensagem = "Pessoa não localizada." });
            }

            await _context.Entry(pessoa).Collection(p => p.Enderecos).LoadAsync();

            if (await _context.Pessoas.AnyAsync(p => p.Cpf == dados.Cpf && p.Idpessoa != id))
            {
                return Conflict(new { mensagem = "Já existe outra pessoa com este CPF." });
            }

            var idsRecebidos = new List<int>();
            foreach (var endereco in dados.Enderecos)
            {
                if (endereco.Idendereco == 0)
                {
                    continue; // ID zero significa endereço novo.
                }

                if (idsRecebidos.Contains(endereco.Idendereco))
                {
                    return BadRequest(new { mensagem = "Não repita o ID de um endereço." });
                }

                if (!pessoa.Enderecos.Any(e => e.Idendereco == endereco.Idendereco))
                {
                    return BadRequest(new { mensagem = "O endereço não existe ou pertence a outra pessoa." });
                }

                idsRecebidos.Add(endereco.Idendereco);
            }

            pessoa.Nome = dados.Nome;
            pessoa.Cpf = dados.Cpf;
            pessoa.DataNascimento = dados.DataNascimento.GetValueOrDefault();
            pessoa.Sexo = dados.Sexo;
            pessoa.Email = dados.Email;
            pessoa.Telefone = dados.Telefone;
            pessoa.AtivoPessoa = dados.AtivoPessoa;

            // Remove somente os endereços antigos que não vieram no PUT.
            foreach (var endereco in pessoa.Enderecos.ToList())
            {
                if (!idsRecebidos.Contains(endereco.Idendereco))
                {
                    _context.Enderecos.Remove(endereco);
                }
            }

            foreach (var dadosEndereco in dados.Enderecos)
            {
                Endereco endereco;
                if (dadosEndereco.Idendereco == 0)
                {
                    endereco = new Endereco
                    {
                        Idpessoa = pessoa.Idpessoa,
                        IdpessoaNavigation = pessoa
                    };
                    _context.Enderecos.Add(endereco);
                }
                else
                {
                    endereco = pessoa.Enderecos.First(e => e.Idendereco == dadosEndereco.Idendereco);
                }

                endereco.Cep = dadosEndereco.Cep;
                endereco.Numero = dadosEndereco.Numero;
                endereco.Complemento = dadosEndereco.Complemento;
            }

            await _context.SaveChangesAsync();
            await transacao.CommitAsync();
            return NoContent();
        }
        catch (DbUpdateException ex)
        {
            await transacao.RollbackAsync();
            _logger.LogError(ex, "Erro ao gravar os dados no banco.");
            return Conflict(new { mensagem = "Não foi possível gravar. Confira os vínculos e se os dados já foram cadastrados." });
        }
    }

    // DELETE: api/PessoaEndereco/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeletePessoaEndereco(int id)
    {
        await using var transacao = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            var registros = await _context.Pessoas
                .FromSqlInterpolated($"SELECT * FROM pessoa WHERE idpessoa = {id} FOR UPDATE")
                .ToListAsync();
            var pessoa = registros.FirstOrDefault();

            if (pessoa == null)
            {
                return NotFound(new { mensagem = "Registro não localizado." });
            }

            if (await _context.Atendimentos.AnyAsync(a => a.Idpessoa == id)
                || await _context.Funcionarios.AnyAsync(f => f.Idpessoa == id))
            {
                return Conflict(new { mensagem = "A pessoa possui atendimentos ou vínculo de funcionário. Desative-a pelo PUT." });
            }

            await _context.Entry(pessoa).Collection(p => p.Enderecos).LoadAsync();
            _context.Enderecos.RemoveRange(pessoa.Enderecos);
            _context.Pessoas.Remove(pessoa);

            await _context.SaveChangesAsync();
            await transacao.CommitAsync();
            return NoContent();
        }
        catch (DbUpdateException ex)
        {
            await transacao.RollbackAsync();
            _logger.LogError(ex, "Erro ao gravar os dados no banco.");
            return Conflict(new { mensagem = "Não foi possível gravar. Confira os vínculos e se os dados já foram cadastrados." });
        }
    }

    // Monta o JSON de saída sem devolver os relacionamentos circulares do EF.
    private object ExibirPessoa(Pessoa pessoa)
    {
        return new
        {
            pessoa.Idpessoa,
            pessoa.Nome,
            pessoa.Cpf,
            pessoa.DataNascimento,
            pessoa.Sexo,
            pessoa.Email,
            pessoa.Telefone,
            pessoa.AtivoPessoa,
            Enderecos = pessoa.Enderecos.OrderBy(e => e.Idendereco).Select(e => new
            {
                e.Idendereco,
                e.Cep,
                e.Numero,
                e.Complemento
            }).ToList()
        };
    }
}
