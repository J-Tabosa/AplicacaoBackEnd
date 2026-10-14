using AplicacaoBackEnd.DTO;
using AplicacaoBackEnd.Models;
using AplicacaoBackEnd.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace AplicacaoBackEnd.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ItemsController : ControllerBase
{
    private readonly DbAtendimentoContext _context;
    private readonly ItemService _itens;
    public ItemsController(DbAtendimentoContext context, ItemService itens) { _context = context; _itens = itens; }

    [HttpGet]
    public async Task<ActionResult<List<ItemRespostaDTO>>> GetItems()
    {
        var itens = await _itens.Consulta().OrderBy(i => i.Iditem).ToListAsync();
        return Ok(itens.Select(ApiMapeamento.Item).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ItemRespostaDTO>> GetItem(int id) => Ok(ApiMapeamento.Item(await _itens.ObterAsync(id)));

    [HttpPost]
    public async Task<ActionResult<ItemRespostaDTO>> PostItem(ItemDTO dados)
    {
        await using var transacao = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        var item = await _itens.CriarAsync(dados);
        await transacao.CommitAsync();
        return CreatedAtAction(nameof(GetItem), new { id = item.Iditem }, ApiMapeamento.Item(item));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> PutItem(int id, ItemDTO dados)
    {
        await using var transacao = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        await _itens.AtualizarAsync(id, dados);
        await transacao.CommitAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteItem(int id)
    {
        await using var transacao = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        await _itens.ExcluirAsync(id);
        await transacao.CommitAsync();
        return NoContent();
    }
}
