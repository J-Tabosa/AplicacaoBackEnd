using AplicacaoBackEnd.DTO;
using AplicacaoBackEnd.Models;
using AplicacaoBackEnd.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace AplicacaoBackEnd.Controllers;

[Route("api/[controller]")]
[ApiController]
public class FuncionarioController : ControllerBase
{
    private readonly DbAtendimentoContext _context;
    private readonly PessoaService _pessoas;
    public FuncionarioController(DbAtendimentoContext context, PessoaService pessoas)
    {
        _context = context;
        _pessoas = pessoas;
    }

    private IQueryable<Funcionario> Consulta() => _context.Funcionarios.AsNoTracking()
        .Include(f => f.IdpessoaNavigation).ThenInclude(p => p.Enderecos);

    [HttpGet("all")]
    public async Task<ActionResult<List<FuncionarioRespostaDTO>>> GetFuncionarios()
    {
        var funcionarios = await Consulta().OrderBy(f => f.Idpessoa).ToListAsync();
        return Ok(funcionarios.Select(ApiMapeamento.Funcionario).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<FuncionarioRespostaDTO>> GetFuncionario(int id)
    {
        var funcionario = await Consulta().FirstOrDefaultAsync(f => f.Idpessoa == id)
            ?? throw new RegraNegocioException(404, "Funcionário não localizado.");
        return Ok(ApiMapeamento.Funcionario(funcionario));
    }

    [HttpPost]
    public async Task<ActionResult<FuncionarioRespostaDTO>> PostFuncionario(FuncionarioCadastroDTO dados)
    {
        var admissao = Validacao.Data(dados.DataAdmissao, "admissão");
        await using var transacao = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        var pessoa = await _pessoas.CriarAsync(dados);
        var funcionario = new Funcionario
        {
            Idpessoa = pessoa.Idpessoa, IdpessoaNavigation = pessoa,
            Cargo = dados.Cargo, DataAdmissao = admissao
        };
        _context.Funcionarios.Add(funcionario);
        await _context.SaveChangesAsync();
        await transacao.CommitAsync();
        return CreatedAtAction(nameof(GetFuncionario), new { id = funcionario.Idpessoa }, ApiMapeamento.Funcionario(funcionario));
    }

    // Atualiza pessoa, endereços e dados profissionais na mesma transação.
    [HttpPut("{id:int}")]
    public async Task<IActionResult> PutFuncionario(int id, FuncionarioCadastroDTO dados)
    {
        var admissao = Validacao.Data(dados.DataAdmissao, "admissão");
        await using var transacao = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        var registros = await _context.Funcionarios
            .FromSqlInterpolated($"SELECT * FROM funcionario WHERE idpessoa = {id} FOR UPDATE").ToListAsync();
        var funcionario = registros.SingleOrDefault()
            ?? throw new RegraNegocioException(404, "Funcionário não localizado.");
        var pessoa = await _pessoas.BloquearAsync(id);
        funcionario.Cargo = dados.Cargo;
        funcionario.DataAdmissao = admissao;
        await _pessoas.AtualizarAsync(pessoa, dados);
        await transacao.CommitAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteFuncionario(int id)
    {
        await using var transacao = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        var registros = await _context.Funcionarios
            .FromSqlInterpolated($"SELECT * FROM funcionario WHERE idpessoa = {id} FOR UPDATE").ToListAsync();
        var funcionario = registros.SingleOrDefault()
            ?? throw new RegraNegocioException(404, "Funcionário não localizado.");
        if (await _context.Atendimentoitems.AnyAsync(a => a.Idfuncionario == id))
            throw new RegraNegocioException(409, "O funcionário possui atendimentos registrados e não pode ser excluído.");
        // A pessoa e seus endereços permanecem cadastrados.
        _context.Funcionarios.Remove(funcionario);
        await _context.SaveChangesAsync();
        await transacao.CommitAsync();
        return NoContent();
    }
}
