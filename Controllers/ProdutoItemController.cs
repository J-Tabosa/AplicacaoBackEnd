using AplicacaoBackEnd.DTO;
using AplicacaoBackEnd.Models;
using AplicacaoBackEnd.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace AplicacaoBackEnd.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProdutoItemController : ControllerBase
{
    private readonly DbAtendimentoContext _context;
    private readonly ItemService _itens;
    public ProdutoItemController(DbAtendimentoContext context, ItemService itens) { _context = context; _itens = itens; }

    private static ItemDTO Converter(ProdutoDTO dados) => new()
    {
        IdCategoria = dados.IdCategoria, Item = dados.Item, DescricaoItem = dados.DescricaoItem,
        Preco = dados.Preco, TipoItem = "P", AtivoProdutoServico = dados.AtivoProdutoServico,
        Produto = new ProdutoDadosDTO { CodigoPreco = dados.CodigoPreco, PrecoVenda = dados.PrecoVenda, Unidade = dados.Unidade }
    };

    [HttpGet]
    public async Task<ActionResult<List<ProdutoRespostaDTO>>> GetProdutos()
    {
        var itens = await _itens.Consulta().Where(i => i.Produto != null).OrderBy(i => i.Iditem).ToListAsync();
        return Ok(itens.Select(i => ApiMapeamento.Produto(i.Produto!)).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProdutoRespostaDTO>> GetProduto(int id)
    {
        var item = await _itens.ObterAsync(id);
        if (item.Produto == null) throw new RegraNegocioException(404, "Produto não localizado.");
        return Ok(ApiMapeamento.Produto(item.Produto));
    }

    [HttpPost]
    public async Task<ActionResult<ProdutoRespostaDTO>> PostProduto(ProdutoDTO dados)
    {
        await using var transacao = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        var item = await _itens.CriarAsync(Converter(dados));
        await transacao.CommitAsync();
        return CreatedAtAction(nameof(GetProduto), new { id = item.Iditem }, ApiMapeamento.Produto(item.Produto!));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> PutProduto(int id, ProdutoDTO dados)
    {
        await using var transacao = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        await _itens.AtualizarAsync(id, Converter(dados), exigirProduto: true);
        await transacao.CommitAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteProduto(int id)
    {
        await using var transacao = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        await _itens.ExcluirAsync(id, exigirProduto: true);
        await transacao.CommitAsync();
        return NoContent();
    }
}
