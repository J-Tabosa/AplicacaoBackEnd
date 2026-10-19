using AplicacaoBackEnd.DTO;
using AplicacaoBackEnd.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace AplicacaoBackEnd.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ItemsController : ControllerBase
{
    private readonly DbAtendimentoContext _context;
    private readonly ILogger<ItemsController> _logger;

    public ItemsController(DbAtendimentoContext context, ILogger<ItemsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetItems()
    {
        var itens = await _context.Items
            .Include(i => i.IdcategoriaNavigation)
            .Include(i => i.Produto)
            .ThenInclude(p => p!.Estoque)
            .AsNoTracking()
            .OrderBy(i => i.Iditem)
            .ToListAsync();

        var resposta = new List<object>();
        foreach (var item in itens)
        {
            resposta.Add(ExibirItem(item));
        }

        return Ok(resposta);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetItem(int id)
    {
        var item = await _context.Items
            .Include(i => i.IdcategoriaNavigation)
            .Include(i => i.Produto)
            .ThenInclude(p => p!.Estoque)
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Iditem == id);

        if (item == null)
        {
            return NotFound(new { mensagem = "Item não localizado." });
        }

        return Ok(ExibirItem(item));
    }

    [HttpPost]
    public async Task<IActionResult> PostItem(ItemDTO dados)
    {
        if (dados.TipoItem == "P" && dados.Produto == null)
        {
            return BadRequest(new { mensagem = "Informe os dados de produto para itens do tipo P." });
        }

        if (dados.TipoItem == "S" && dados.Produto != null)
        {
            return BadRequest(new { mensagem = "Serviços não possuem dados de produto ou estoque." });
        }

        if (!await _context.Categoria.AnyAsync(c => c.Idcategoria == dados.IdCategoria))
        {
            return NotFound(new { mensagem = "Categoria não localizada." });
        }

        await using var transacao = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            var item = new Item();
            item.Idcategoria = dados.IdCategoria;
            item.Item1 = dados.Item;
            item.DescricaoItem = dados.DescricaoItem;
            item.Preco = dados.Preco;
            item.TipoItem = dados.TipoItem;
            item.AtivoProdutoServico = dados.AtivoProdutoServico;

            _context.Items.Add(item);
            await _context.SaveChangesAsync();

            if (dados.TipoItem == "P" && dados.Produto != null)
            {
                var produto = new Produto
                {
                    Iditem = item.Iditem,
                    IditemNavigation = item
                };
                produto.CodigoPreco = dados.Produto.CodigoPreco;
                produto.PrecoVenda = dados.Produto.PrecoVenda;
                produto.Unidade = dados.Produto.Unidade;

                _context.Produtos.Add(produto);
                // A trigger de produto cria o estoque zerado.
                await _context.SaveChangesAsync();
            }

            var itemCadastrado = await _context.Items
                .Include(i => i.IdcategoriaNavigation)
                .Include(i => i.Produto)
                .ThenInclude(p => p!.Estoque)
                .AsNoTracking()
                .FirstAsync(i => i.Iditem == item.Iditem);

            await transacao.CommitAsync();
            return CreatedAtAction(nameof(GetItem), new { id = item.Iditem }, ExibirItem(itemCadastrado));
        }
        catch (DbUpdateException ex)
        {
            await transacao.RollbackAsync();
            _logger.LogError(ex, "Erro ao gravar os dados no banco.");
            return Conflict(new { mensagem = "Não foi possível gravar. Confira os vínculos e se os dados já foram cadastrados." });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> PutItem(int id, ItemDTO dados)
    {
        if (dados.TipoItem == "P" && dados.Produto == null)
        {
            return BadRequest(new { mensagem = "Informe os dados de produto para itens do tipo P." });
        }

        if (dados.TipoItem == "S" && dados.Produto != null)
        {
            return BadRequest(new { mensagem = "Serviços não possuem dados de produto ou estoque." });
        }

        await using var transacao = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            var registros = await _context.Items
                .FromSqlInterpolated($"SELECT * FROM item WHERE iditem = {id} FOR UPDATE")
                .ToListAsync();
            var item = registros.FirstOrDefault();

            if (item == null)
            {
                return NotFound(new { mensagem = "Item não localizado." });
            }

            await _context.Entry(item).Reference(i => i.Produto).LoadAsync();

            if (item.TipoItem != dados.TipoItem)
            {
                return Conflict(new { mensagem = "Não é permitido converter produto em serviço ou serviço em produto." });
            }

            if (!await _context.Categoria.AnyAsync(c => c.Idcategoria == dados.IdCategoria))
            {
                return NotFound(new { mensagem = "Categoria não localizada." });
            }

            item.Idcategoria = dados.IdCategoria;
            item.Item1 = dados.Item;
            item.DescricaoItem = dados.DescricaoItem;
            item.Preco = dados.Preco;
            item.TipoItem = dados.TipoItem;
            item.AtivoProdutoServico = dados.AtivoProdutoServico;

            if (dados.TipoItem == "P")
            {
                var produto = item.Produto;
                if (produto == null)
                {
                    return Conflict(new { mensagem = "O item não possui cadastro de produto." });
                }

                if (dados.Produto == null)
                {
                    return BadRequest(new { mensagem = "Informe os dados de produto." });
                }

                produto.CodigoPreco = dados.Produto.CodigoPreco;
                produto.PrecoVenda = dados.Produto.PrecoVenda;
                produto.Unidade = dados.Produto.Unidade;
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

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteItem(int id)
    {
        await using var transacao = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            var registros = await _context.Items
                .FromSqlInterpolated($"SELECT * FROM item WHERE iditem = {id} FOR UPDATE")
                .ToListAsync();
            var item = registros.FirstOrDefault();

            if (item == null)
            {
                return NotFound(new { mensagem = "Item não localizado." });
            }

            await _context.Entry(item).Reference(i => i.Produto).LoadAsync();

            if (await _context.Atendimentoitems.AnyAsync(a => a.Iditem == id)
                || await _context.EntradaProdutos.AnyAsync(e => e.Iditem == id))
            {
                return Conflict(new { mensagem = "O item possui atendimentos ou entradas. Desative-o pelo PUT." });
            }

            if (item.Produto != null)
            {
                var estoques = await _context.Estoques
                    .FromSqlInterpolated($"SELECT * FROM estoque WHERE iditem = {id} FOR UPDATE")
                    .ToListAsync();
                var estoque = estoques.FirstOrDefault();

                if (estoque == null)
                {
                    return Conflict(new { mensagem = "Produto sem estoque. Confira a trigger de cadastro." });
                }

                if (estoque.QuantidadeEstoque != 0)
                {
                    return Conflict(new { mensagem = "O produto ainda possui saldo em estoque." });
                }

                _context.Estoques.Remove(estoque);
                _context.Produtos.Remove(item.Produto);
            }

            _context.Items.Remove(item);
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

    private object ExibirItem(Item item)
    {
        object? produto = null;
        if (item.Produto != null)
        {
            produto = new
            {
                item.Produto.CodigoPreco,
                item.Produto.PrecoVenda,
                item.Produto.Unidade,
                QuantidadeEstoque = item.Produto.Estoque?.QuantidadeEstoque
            };
        }

        return new
        {
            IdItem = item.Iditem,
            IdCategoria = item.Idcategoria,
            Categoria = item.IdcategoriaNavigation.Categoria,
            Item = item.Item1,
            item.DescricaoItem,
            item.Preco,
            item.TipoItem,
            item.AtivoProdutoServico,
            Produto = produto
        };
    }
}
