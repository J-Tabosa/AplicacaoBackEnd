using AplicacaoBackEnd.DTO;
using AplicacaoBackEnd.Models;
using AplicacaoBackEnd.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace AplicacaoBackEnd.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AtendimentoController : ControllerBase
{
    private readonly DbAtendimentoContext _context;
    private readonly AtendimentoService _atendimentos;
    public AtendimentoController(DbAtendimentoContext context, AtendimentoService atendimentos)
    {
        _context = context;
        _atendimentos = atendimentos;
    }

    [HttpGet("all")]
    public async Task<ActionResult<List<AtendimentoRespostaDTO>>> GetAtendimentos()
    {
        var atendimentos = await _atendimentos.Consulta().OrderBy(a => a.Idatendimento).ToListAsync();
        return Ok(atendimentos.Select(ApiMapeamento.Atendimento).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AtendimentoRespostaDTO>> GetAtendimento(int id) => Ok(await _atendimentos.ObterRespostaAsync(id));

    [HttpPost]
    public async Task<ActionResult<AtendimentoRespostaDTO>> PostAtendimento(AtendimentoDTO dados)
    {
        var data = Validacao.Data(dados.DataAtendimento, "atendimento");
        await using var transacao = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        await _atendimentos.ConferirClienteAsync(dados.IdPessoa);
        var atendimento = new Atendimento { Idpessoa = dados.IdPessoa, DataAtendimento = data, StatusAtendimento = dados.StatusAtendimento };
        _context.Atendimentos.Add(atendimento);
        await _context.SaveChangesAsync();
        await _atendimentos.SincronizarItensAsync(atendimento, dados.Itens);
        var resposta = await _atendimentos.ObterRespostaAsync(atendimento.Idatendimento);
        await transacao.CommitAsync();
        return CreatedAtAction(nameof(GetAtendimento), new { id = atendimento.Idatendimento }, resposta);
    }

    // A lista enviada no PUT representa todos os itens que devem permanecer.
    [HttpPut("{id:int}")]
    public async Task<IActionResult> PutAtendimento(int id, AtendimentoDTO dados)
    {
        var data = Validacao.Data(dados.DataAtendimento, "atendimento");
        await using var transacao = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        var atendimento = await _atendimentos.BloquearAsync(id);
        await _atendimentos.ConferirClienteAsync(dados.IdPessoa);
        atendimento.Idpessoa = dados.IdPessoa;
        atendimento.DataAtendimento = data;
        atendimento.StatusAtendimento = dados.StatusAtendimento;
        await _atendimentos.SincronizarItensAsync(atendimento, dados.Itens);
        await transacao.CommitAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAtendimento(int id)
    {
        await using var transacao = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        var atendimento = await _atendimentos.BloquearAsync(id);
        await _atendimentos.SincronizarItensAsync(atendimento, new List<AtendimentoItemDTO>(), permitirVazio: true);
        _context.Atendimentos.Remove(atendimento);
        await _context.SaveChangesAsync();
        await transacao.CommitAsync();
        return NoContent();
    }

    [HttpPost("{id:int}/itens")]
    public async Task<ActionResult<AtendimentoRespostaDTO>> PostItem(int id, AtendimentoItemDTO dados)
    {
        await using var transacao = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        var atendimento = await _atendimentos.BloquearAsync(id);
        var itens = AtendimentoService.ExtrairItens(atendimento);
        if (itens.Any(i => i.IdItem == dados.IdItem))
            throw new RegraNegocioException(409, "O item já pertence ao atendimento. Use o PUT para alterar a quantidade.");
        itens.Add(dados);
        await _atendimentos.SincronizarItensAsync(atendimento, itens);
        var resposta = await _atendimentos.ObterRespostaAsync(id);
        await transacao.CommitAsync();
        return CreatedAtAction(nameof(GetAtendimento), new { id }, resposta);
    }

    [HttpPut("{id:int}/itens/{idItem:int}")]
    public async Task<IActionResult> PutItem(int id, int idItem, AtendimentoItemDTO dados)
    {
        if (idItem != dados.IdItem)
            throw new RegraNegocioException(400, "O ID do item da URL deve corresponder ao JSON.");
        await using var transacao = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        var atendimento = await _atendimentos.BloquearAsync(id);
        var itens = AtendimentoService.ExtrairItens(atendimento);
        var indice = itens.FindIndex(i => i.IdItem == idItem);
        if (indice < 0) throw new RegraNegocioException(404, "Item não localizado neste atendimento.");
        itens[indice] = dados;
        await _atendimentos.SincronizarItensAsync(atendimento, itens);
        await transacao.CommitAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}/itens/{idItem:int}")]
    public async Task<IActionResult> DeleteItem(int id, int idItem)
    {
        await using var transacao = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        var atendimento = await _atendimentos.BloquearAsync(id);
        var itens = AtendimentoService.ExtrairItens(atendimento);
        if (itens.RemoveAll(i => i.IdItem == idItem) == 0)
            throw new RegraNegocioException(404, "Item não localizado neste atendimento.");
        if (itens.Count == 0)
            throw new RegraNegocioException(409, "O atendimento deve ter pelo menos um item. Para remover tudo, exclua o atendimento.");
        await _atendimentos.SincronizarItensAsync(atendimento, itens);
        await transacao.CommitAsync();
        return NoContent();
    }
}
