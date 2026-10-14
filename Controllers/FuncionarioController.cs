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
    public async Task<IActionResult> PostFuncionario(FuncionarioDTO dados)
    {
        if (dados.DataAdmissao < new DateOnly(1000, 1, 1))
            return BadRequest(new { mensagem = "INFORME UMA DATA DE ADMISSÃO VÁLIDA." });

        var pessoa = await _context.Pessoas
            .Include(p => p.Enderecos)
            .FirstOrDefaultAsync(p => p.Idpessoa == dados.IdPessoa);

        if (pessoa == null)
            return NotFound(new { mensagem = "CADASTRE A PESSOA ANTES DE REGISTRAR O FUNCIONÁRIO." });

        if (await _context.Funcionarios.AnyAsync(f => f.Idpessoa == dados.IdPessoa))
            return Conflict(new { mensagem = "ESTA PESSOA JÁ ESTÁ CADASTRADA COMO FUNCIONÁRIO." });

        var funcionario = new Funcionario
        {
            Idpessoa = pessoa.Idpessoa,
            IdpessoaNavigation = pessoa,
            Cargo = dados.Cargo,
            DataAdmissao = dados.DataAdmissao.GetValueOrDefault()
        };

        try
        {
            _context.Funcionarios.Add(funcionario);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetFuncionario),
                new { id = funcionario.Idpessoa },
                ExibirFuncionario(funcionario));
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Erro ao cadastrar o funcionário {Id}.", dados.IdPessoa);

            if (await _context.Funcionarios.AsNoTracking().AnyAsync(f => f.Idpessoa == dados.IdPessoa))
                return Conflict(new { mensagem = "ESTA PESSOA JÁ ESTÁ CADASTRADA COMO FUNCIONÁRIO." });

            if (!await _context.Pessoas.AsNoTracking().AnyAsync(p => p.Idpessoa == dados.IdPessoa))
                return NotFound(new { mensagem = "PESSOA NÃO LOCALIZADA!" });

            return StatusCode(500, new { mensagem = "ERRO NO CADASTRO DO FUNCIONÁRIO." });
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
