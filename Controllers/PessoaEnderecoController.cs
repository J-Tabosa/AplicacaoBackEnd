using AplicacaoBackEnd.DTO;
using AplicacaoBackEnd.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AplicacaoBackEnd.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PessoaEnderecoController : ControllerBase
{
    private readonly DbAtendimentoContext _context;
    private readonly ILogger<PessoaEnderecoController> _logger;

    public PessoaEnderecoController(
        DbAtendimentoContext context,
        ILogger<PessoaEnderecoController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/PessoaEndereco/all
    [HttpGet("all")]
    public async Task<IActionResult> GetPessoasEnderecos()
    {
        var pessoas = await _context.Pessoas
            .AsNoTracking()
            .OrderBy(p => p.Idpessoa)
            .Select(p => new
            {
                p.Idpessoa,
                p.Nome,
                p.Cpf,
                p.DataNascimento,
                p.Sexo,
                p.Email,
                p.Telefone,
                p.AtivoPessoa,
                Enderecos = p.Enderecos
                    .OrderBy(e => e.Idendereco)
                    .Select(e => new
                    {
                        e.Idendereco,
                        e.Cep,
                        e.Numero,
                        e.Complemento
                    }).ToList()
            })
            .ToListAsync();

        return Ok(pessoas);
    }

    // GET: api/PessoaEndereco/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetPessoaEndereco(int id)
    {
        // Uma pessoa pode ter vários endereços. A projeção evita ciclos no JSON.
        var pessoa = await _context.Pessoas
            .AsNoTracking()
            .Where(p => p.Idpessoa == id)
            .Select(p => new
            {
                p.Idpessoa,
                p.Nome,
                p.Cpf,
                p.DataNascimento,
                p.Sexo,
                p.Email,
                p.Telefone,
                p.AtivoPessoa,
                Enderecos = p.Enderecos
                    .OrderBy(e => e.Idendereco)
                    .Select(e => new
                    {
                        e.Idendereco,
                        e.Cep,
                        e.Numero,
                        e.Complemento
                    }).ToList()
            })
            .FirstOrDefaultAsync();

        if (pessoa == null)
        {
            return NotFound(new { mensagem = "PESSOA NÃO LOCALIZADA!" });
        }

        return Ok(pessoa);
    }

    // POST: api/PessoaEndereco
    [HttpPost]
    public async Task<IActionResult> PostPessoaEndereco(PessoaEnderecoDTO dados)
    {
        if (dados.Enderecos.Any(e => e == null || e.Idendereco != 0))
        {
            return BadRequest(new { mensagem = "ENVIE ENDEREÇOS NOVOS, SEM ID OU COM ID IGUAL A ZERO." });
        }

        if (dados.DataNascimento == DateOnly.MinValue)
        {
            return BadRequest(new { mensagem = "INFORME UMA DATA DE NASCIMENTO VÁLIDA." });
        }

        if (await _context.Pessoas.AnyAsync(p => p.Cpf == dados.Cpf))
        {
            return Conflict(new { mensagem = "JÁ EXISTE UMA PESSOA COM ESTE CPF." });
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var pessoa = new Pessoa();
            AplicarDadosPessoa(pessoa, dados);

            foreach (var dadosEndereco in dados.Enderecos)
            {
                var endereco = new Endereco();
                AplicarDadosEndereco(endereco, dadosEndereco);
                pessoa.Enderecos.Add(endereco);
            }

            // O EF gera o ID da pessoa e o usa em todos os endereços.
            _context.Pessoas.Add(pessoa);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return CreatedAtAction(
                nameof(GetPessoaEndereco),
                new { id = pessoa.Idpessoa },
                ExibirPessoa(pessoa));
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Erro ao cadastrar pessoa e endereços.");

            if (await _context.Pessoas.AsNoTracking().AnyAsync(p => p.Cpf == dados.Cpf))
                return Conflict(new { mensagem = "JÁ EXISTE UMA PESSOA COM ESTE CPF." });

            return StatusCode(500, new { mensagem = "ERRO NO CADASTRO DA PESSOA E DOS ENDEREÇOS." });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Erro ao cadastrar pessoa e endereços.");
            return StatusCode(500, new { mensagem = "ERRO NO CADASTRO DA PESSOA E DOS ENDEREÇOS." });
        }
    }

    // PUT: api/PessoaEndereco/5
    // Envie todos os endereços que devem permanecer. Os omitidos serão removidos.
    [HttpPut("{id:int}")]
    public async Task<IActionResult> PutPessoaEndereco(int id, PessoaEnderecoDTO dados)
    {
        var pessoa = await _context.Pessoas
            .Include(p => p.Enderecos)
            .FirstOrDefaultAsync(p => p.Idpessoa == id);

        if (pessoa == null)
            return NotFound(new { mensagem = "PESSOA NÃO LOCALIZADA!" });

        if (dados.Enderecos.Any(e => e == null))
            return BadRequest(new { mensagem = "A LISTA NÃO PODE CONTER ENDEREÇOS NULOS." });

        if (dados.DataNascimento == DateOnly.MinValue)
            return BadRequest(new { mensagem = "INFORME UMA DATA DE NASCIMENTO VÁLIDA." });

        var idsEnviados = dados.Enderecos
            .Where(e => e.Idendereco > 0)
            .Select(e => e.Idendereco)
            .ToList();

        if (idsEnviados.Distinct().Count() != idsEnviados.Count)
            return BadRequest(new { mensagem = "NÃO REPITA O ID DE UM ENDEREÇO." });

        var existentes = pessoa.Enderecos.ToDictionary(e => e.Idendereco);

        if (idsEnviados.Any(idEndereco => !existentes.ContainsKey(idEndereco)))
            return BadRequest(new { mensagem = "UM DOS ENDEREÇOS NÃO PERTENCE A ESTA PESSOA OU NÃO EXISTE." });

        if (await _context.Pessoas.AnyAsync(p => p.Cpf == dados.Cpf && p.Idpessoa != id))
            return Conflict(new { mensagem = "JÁ EXISTE OUTRA PESSOA COM ESTE CPF." });

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            AplicarDadosPessoa(pessoa, dados);

            var removidos = pessoa.Enderecos
                .Where(e => !idsEnviados.Contains(e.Idendereco))
                .ToList();
            _context.Enderecos.RemoveRange(removidos);

            foreach (var dadosEndereco in dados.Enderecos)
            {
                if (dadosEndereco.Idendereco == 0)
                {
                    var endereco = new Endereco
                    {
                        Idpessoa = pessoa.Idpessoa,
                        IdpessoaNavigation = pessoa
                    };
                    AplicarDadosEndereco(endereco, dadosEndereco);
                    _context.Enderecos.Add(endereco);
                }
                else
                {
                    AplicarDadosEndereco(existentes[dadosEndereco.Idendereco], dadosEndereco);
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return NoContent();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await transaction.RollbackAsync();
            _logger.LogWarning(ex, "Conflito ao atualizar a pessoa {Id}.", id);
            return Conflict(new { mensagem = "OS DADOS FORAM ALTERADOS OU EXCLUÍDOS. CONSULTE NOVAMENTE." });
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Erro ao atualizar a pessoa {Id}.", id);

            if (await _context.Pessoas.AsNoTracking().AnyAsync(p => p.Cpf == dados.Cpf && p.Idpessoa != id))
                return Conflict(new { mensagem = "JÁ EXISTE OUTRA PESSOA COM ESTE CPF." });

            return StatusCode(500, new { mensagem = "ERRO NA ATUALIZAÇÃO DA PESSOA E DOS ENDEREÇOS." });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Erro ao atualizar a pessoa {Id}.", id);
            return StatusCode(500, new { mensagem = "ERRO NA ATUALIZAÇÃO DA PESSOA E DOS ENDEREÇOS." });
        }
    }

    // DELETE: api/PessoaEndereco/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeletePessoaEndereco(int id)
    {
        var pessoa = await _context.Pessoas
            .Include(p => p.Enderecos)
            .FirstOrDefaultAsync(p => p.Idpessoa == id);

        if (pessoa == null)
            return NotFound(new { mensagem = "PESSOA NÃO LOCALIZADA!" });

        if (await _context.Atendimentos.AnyAsync(a => a.Idpessoa == id)
            || await _context.Funcionarios.AnyAsync(f => f.Idpessoa == id))
        {
            return Conflict(new
            {
                mensagem = "A PESSOA POSSUI ATENDIMENTOS OU CADASTRO DE FUNCIONÁRIO. DESATIVE-A PELO PUT EM VEZ DE EXCLUIR."
            });
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            _context.Enderecos.RemoveRange(pessoa.Enderecos);
            _context.Pessoas.Remove(pessoa);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return NoContent();
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync();
            _logger.LogWarning(ex, "Conflito ao excluir a pessoa {Id}.", id);
            return Conflict(new { mensagem = "NÃO FOI POSSÍVEL EXCLUIR A PESSOA. CONSULTE SEUS VÍNCULOS." });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Erro ao excluir a pessoa {Id}.", id);
            return StatusCode(500, new { mensagem = "ERRO NA EXCLUSÃO DA PESSOA E DOS ENDEREÇOS." });
        }
    }

    private static void AplicarDadosPessoa(Pessoa pessoa, PessoaEnderecoDTO dados)
    {
        pessoa.Nome = dados.Nome;
        pessoa.Cpf = dados.Cpf;
        pessoa.DataNascimento = dados.DataNascimento.GetValueOrDefault();
        pessoa.Sexo = dados.Sexo;
        pessoa.Email = dados.Email;
        pessoa.Telefone = dados.Telefone;
        pessoa.AtivoPessoa = dados.AtivoPessoa;
    }

    private static void AplicarDadosEndereco(Endereco endereco, EnderecoDTO dados)
    {
        endereco.Cep = dados.Cep;
        endereco.Numero = dados.Numero;
        endereco.Complemento = dados.Complemento;
    }

    private static object ExibirPessoa(Pessoa pessoa) => new
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
