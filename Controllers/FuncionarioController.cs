using AplicacaoBackEnd.DTO;
using AplicacaoBackEnd.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AplicacaoBackEnd.Controllers;

[Route("api/[controller]")]
[ApiController]
public class FuncionarioController : ControllerBase
{
    private readonly DbAtendimentoContext _context;
    private readonly ILogger<FuncionarioController> _logger;

    public FuncionarioController(
        DbAtendimentoContext context,
        ILogger<FuncionarioController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/Funcionario/all
    [HttpGet("all")]
    public async Task<IActionResult> GetFuncionarios()
    {
        var funcionarios = await ConsultarFuncionarios()
            .AsNoTracking()
            .OrderBy(f => f.Idpessoa)
            .ToListAsync();

        return Ok(funcionarios.Select(ExibirFuncionario));
    }

    // GET: api/Funcionario/5 (o ID é o mesmo da pessoa)
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetFuncionario(int id)
    {
        var funcionario = await ConsultarFuncionarios()
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Idpessoa == id);

        if (funcionario == null)
            return NotFound(new { mensagem = "FUNCIONÁRIO NÃO LOCALIZADO!" });

        return Ok(ExibirFuncionario(funcionario));
    }

    // POST: api/Funcionario
    [HttpPost]
    public async Task<IActionResult> PostFuncionario(FuncionarioCadastroDTO dados)
    {
        if (dados.DataAdmissao < new DateOnly(1000, 1, 1))
            return BadRequest(new { mensagem = "INFORME UMA DATA DE ADMISSÃO VÁLIDA." });

        if (dados.DataNascimento < new DateOnly(1000, 1, 1))
            return BadRequest(new { mensagem = "INFORME UMA DATA DE NASCIMENTO VÁLIDA." });

        if (dados.Enderecos.Any(e => e == null || e.Idendereco != 0))
            return BadRequest(new { mensagem = "ENVIE ENDEREÇOS NOVOS, SEM ID OU COM ID IGUAL A ZERO." });

        if (await _context.Pessoas.AnyAsync(p => p.Cpf == dados.Cpf))
            return Conflict(new { mensagem = "JÁ EXISTE UMA PESSOA COM ESTE CPF." });

        // As três tabelas devem ser cadastradas juntas ou nenhuma será gravada.
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var pessoa = new Pessoa
            {
                Nome = dados.Nome,
                Cpf = dados.Cpf,
                DataNascimento = dados.DataNascimento.GetValueOrDefault(),
                Sexo = dados.Sexo,
                Email = dados.Email,
                Telefone = dados.Telefone,
                AtivoPessoa = dados.AtivoPessoa
            };

            foreach (var dadosEndereco in dados.Enderecos)
            {
                pessoa.Enderecos.Add(new Endereco
                {
                    Cep = dadosEndereco.Cep,
                    Numero = dadosEndereco.Numero,
                    Complemento = dadosEndereco.Complemento
                });
            }

            // O EF gera o ID da pessoa e o utiliza em seus endereços.
            _context.Pessoas.Add(pessoa);
            await _context.SaveChangesAsync();

            // Usa o ID recém-gerado para cadastrar o funcionário.
            var funcionario = new Funcionario
            {
                Idpessoa = pessoa.Idpessoa,
                IdpessoaNavigation = pessoa,
                Cargo = dados.Cargo,
                DataAdmissao = dados.DataAdmissao.GetValueOrDefault()
            };

            _context.Funcionarios.Add(funcionario);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return CreatedAtAction(
                nameof(GetFuncionario),
                new { id = funcionario.Idpessoa },
                ExibirFuncionario(funcionario));
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Erro ao cadastrar pessoa, endereços e funcionário.");

            if (await _context.Pessoas.AsNoTracking().AnyAsync(p => p.Cpf == dados.Cpf))
                return Conflict(new { mensagem = "JÁ EXISTE UMA PESSOA COM ESTE CPF." });

            return StatusCode(500, new { mensagem = "ERRO NO CADASTRO DA PESSOA, DOS ENDEREÇOS E DO FUNCIONÁRIO." });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Erro ao cadastrar pessoa, endereços e funcionário.");
            return StatusCode(500, new { mensagem = "ERRO NO CADASTRO DA PESSOA, DOS ENDEREÇOS E DO FUNCIONÁRIO." });
        }
    }

    // PUT: api/Funcionario/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> PutFuncionario(int id, FuncionarioDTO dados)
    {
        if (id != dados.IdPessoa)
            return BadRequest(new { mensagem = "O ID DA URL DEVE SER IGUAL AO ID DA PESSOA NO JSON." });

        if (dados.DataAdmissao < new DateOnly(1000, 1, 1))
            return BadRequest(new { mensagem = "INFORME UMA DATA DE ADMISSÃO VÁLIDA." });

        var funcionario = await _context.Funcionarios.FindAsync(id);

        if (funcionario == null)
            return NotFound(new { mensagem = "FUNCIONÁRIO NÃO LOCALIZADO!" });

        // Dados pessoais e endereços são atualizados em PessoaEndereco.
        funcionario.Cargo = dados.Cargo;
        funcionario.DataAdmissao = dados.DataAdmissao.GetValueOrDefault();

        try
        {
            await _context.SaveChangesAsync();
            return NoContent();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Conflito ao atualizar o funcionário {Id}.", id);
            return Conflict(new { mensagem = "O FUNCIONÁRIO FOI ALTERADO OU EXCLUÍDO. CONSULTE NOVAMENTE." });
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Erro ao atualizar o funcionário {Id}.", id);
            return StatusCode(500, new { mensagem = "ERRO NA ATUALIZAÇÃO DO FUNCIONÁRIO." });
        }
    }

    // DELETE: api/Funcionario/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteFuncionario(int id)
    {
        var funcionario = await _context.Funcionarios.FindAsync(id);

        if (funcionario == null)
            return NotFound(new { mensagem = "FUNCIONÁRIO NÃO LOCALIZADO!" });

        if (await _context.Atendimentoitems.AnyAsync(a => a.Idfuncionario == id))
            return Conflict(new { mensagem = "O FUNCIONÁRIO POSSUI ATENDIMENTOS REGISTRADOS E NÃO PODE SER EXCLUÍDO." });

        try
        {
            // Remove o vínculo de funcionário e preserva a pessoa e seus endereços.
            _context.Funcionarios.Remove(funcionario);
            await _context.SaveChangesAsync();
            return NoContent();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Conflito ao excluir o funcionário {Id}.", id);
            return Conflict(new { mensagem = "O FUNCIONÁRIO FOI ALTERADO OU EXCLUÍDO. CONSULTE NOVAMENTE." });
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "Erro ao excluir o funcionário {Id}.", id);
            return Conflict(new { mensagem = "NÃO FOI POSSÍVEL EXCLUIR O FUNCIONÁRIO. CONSULTE SEUS VÍNCULOS." });
        }
    }

    // Carrega funcionário -> pessoa -> lista de endereços.
    private IQueryable<Funcionario> ConsultarFuncionarios() => _context.Funcionarios
        .Include(f => f.IdpessoaNavigation)
        .ThenInclude(p => p.Enderecos);

    // Define o JSON sem expor os relacionamentos circulares das entidades.
    private static object ExibirFuncionario(Funcionario funcionario)
    {
        var pessoa = funcionario.IdpessoaNavigation;

        return new
        {
            idPessoa = funcionario.Idpessoa,
            funcionario.Cargo,
            funcionario.DataAdmissao,
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
