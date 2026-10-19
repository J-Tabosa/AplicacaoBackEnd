using AplicacaoBackEnd.DTO;
using AplicacaoBackEnd.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace AplicacaoBackEnd.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ProdutoItemController : ControllerBase
{
    private readonly DbAtendimentoContext _context;
    private readonly ILogger<ProdutoItemController> _logger;

    public ProdutoItemController(DbAtendimentoContext context, ILogger<ProdutoItemController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetProdutos()
    {
        var itens = await _context.Items
            .Include(i => i.IdcategoriaNavigation)
            .Include(i => i.Produto)
            .Where(i => i.Produto != null)
            .AsNoTracking()
            .OrderBy(i => i.Iditem)
            .ToListAsync();

        var resposta = new List<object>();
        foreach (var item in itens)
        {
            resposta.Add(ExibirProduto(item));
        }

        return Ok(resposta);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetProduto(int id)
    {
        var item = await _context.Items
            .Include(i => i.IdcategoriaNavigation)
            .Include(i => i.Produto)
            .Where(i => i.Produto != null)
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Iditem == id);

        if (item == null)
        {
            return NotFound(new { mensagem = "Produto não localizado." });
        }

        return Ok(ExibirProduto(item));
    }

    [HttpPost]
    public async Task<IActionResult> PostProduto(ProdutoDTO dados)
    {
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

            if (dados.TipoItem == "P")
            {
                var produto = new Produto
                {
                    Iditem = item.Iditem,
                    IditemNavigation = item
                };
                produto.CodigoPreco = dados.CodigoPreco;
                produto.PrecoVenda = dados.PrecoVenda;
                produto.Unidade = dados.Unidade;

                _context.Produtos.Add(produto);
                // A trigger de produto cria o estoque zerado.
                await _context.SaveChangesAsync();
            }

            var itemCadastrado = await _context.Items
                .Include(i => i.IdcategoriaNavigation)
                .Include(i => i.Produto)
                .Where(i => i.Produto != null)
                .AsNoTracking()
                .FirstAsync(i => i.Iditem == item.Iditem);

            await transacao.CommitAsync();
            return CreatedAtAction(nameof(GetProduto), new { id = item.Iditem }, ExibirProduto(itemCadastrado));
        }
        catch (DbUpdateException ex)
        {
            await transacao.RollbackAsync();
            _logger.LogError(ex, "Erro ao gravar os dados no banco.");
            return Conflict(new { mensagem = "Não foi possível gravar. Confira os vínculos e se os dados já foram cadastrados." });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> PutProduto(int id, ProdutoDTO dados)
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

            if (item.Produto == null)
            {
                return NotFound(new { mensagem = "Produto não localizado." });
            }

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

                produto.CodigoPreco = dados.CodigoPreco;
                produto.PrecoVenda = dados.PrecoVenda;
                produto.Unidade = dados.Unidade;
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
    public async Task<IActionResult> DeleteProduto(int id)
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

            if (item.Produto == null)
            {
                return NotFound(new { mensagem = "Produto não localizado." });
            }

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

    private object ExibirProduto(Item item)
    {
        return new
        {
            IdItem = item.Iditem,
            IdCategoria = item.Idcategoria,
            Item = item.Item1,
            item.DescricaoItem,
            item.Preco,
            item.TipoItem,
            item.AtivoProdutoServico,
            CodigoPreco = item.Produto?.CodigoPreco,
            PrecoVenda = item.Produto?.PrecoVenda,
            Unidade = item.Produto?.Unidade
        };
    }
}
