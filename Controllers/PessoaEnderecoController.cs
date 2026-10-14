using AplicacaoBackEnd.DTO;
using AplicacaoBackEnd.Models;
using AplicacaoBackEnd.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace AplicacaoBackEnd.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PessoaEnderecoController : ControllerBase
{
    private readonly DbAtendimentoContext _context;
    private readonly PessoaService _pessoas;
    public PessoaEnderecoController(DbAtendimentoContext context, PessoaService pessoas)
    {
        _context = context;
        _pessoas = pessoas;
    }

    [HttpGet("all")]
    public async Task<ActionResult<List<PessoaRespostaDTO>>> GetPessoasEnderecos()
    {
        var pessoas = await _pessoas.Consulta().OrderBy(p => p.Idpessoa).ToListAsync();
        return Ok(pessoas.Select(ApiMapeamento.Pessoa).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PessoaRespostaDTO>> GetPessoaEndereco(int id)
    {
        var pessoa = await _pessoas.Consulta().FirstOrDefaultAsync(p => p.Idpessoa == id)
            ?? throw new RegraNegocioException(404, "Pessoa não localizada.");
        return Ok(ApiMapeamento.Pessoa(pessoa));
    }

    [HttpPost]
    public async Task<ActionResult<PessoaRespostaDTO>> PostPessoaEndereco(PessoaEnderecoDTO dados)
    {
        await using var transacao = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        var pessoa = await _pessoas.CriarAsync(dados);
        await transacao.CommitAsync();
        return CreatedAtAction(nameof(GetPessoaEndereco), new { id = pessoa.Idpessoa }, ApiMapeamento.Pessoa(pessoa));
    }

    // A lista enviada é a lista final: IDs omitidos serão removidos.
    [HttpPut("{id:int}")]
    public async Task<IActionResult> PutPessoaEndereco(int id, PessoaEnderecoDTO dados)
    {
        await using var transacao = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        var pessoa = await _pessoas.BloquearAsync(id);
        await _pessoas.AtualizarAsync(pessoa, dados);
        await transacao.CommitAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeletePessoaEndereco(int id)
    {
        await using var transacao = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        var pessoa = await _pessoas.BloquearAsync(id);
        if (await _context.Atendimentos.AnyAsync(a => a.Idpessoa == id)
            || await _context.Funcionarios.AnyAsync(f => f.Idpessoa == id))
            throw new RegraNegocioException(409, "A pessoa possui atendimentos ou vínculo de funcionário. Desative-a pelo PUT.");
        _context.Enderecos.RemoveRange(pessoa.Enderecos);
        _context.Pessoas.Remove(pessoa);
        await _context.SaveChangesAsync();
        await transacao.CommitAsync();
        return NoContent();
    }
}
