using AplicacaoBackEnd.DTO;
using AplicacaoBackEnd.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AplicacaoBackEnd.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ProdutoItemController : ControllerBase
{
    private readonly DbAtendimentoContext _context;
    private readonly ILogger<ProdutoItemController> _logger;

    public ProdutoItemController(
        DbAtendimentoContext context,
        ILogger<ProdutoItemController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/ProdutoItem
    [HttpGet]
    public async Task<IActionResult> GetProdutos()
    {
        var produtos = await _context.Produtos
            .AsNoTracking()
            .Include(p => p.IditemNavigation)
            .ToListAsync();

        return Ok(produtos.Select(ExibirProduto));
    }

    // GET: api/ProdutoItem/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetProduto(int id)
    {
        var produto = await _context.Produtos
            .AsNoTracking()
            .Include(p => p.IditemNavigation)
            .FirstOrDefaultAsync(p => p.Iditem == id);

        if (produto == null)
        {
            return NotFound(new { mensagem = "PRODUTO NÃO LOCALIZADO!" });
        }

        return Ok(ExibirProduto(produto));
    }

    // POST: api/ProdutoItem
    [HttpPost]
    public async Task<IActionResult> PostProduto(ProdutoDTO produtoDTO)
    {
        if (!await _context.Categoria.AnyAsync(c => c.Idcategoria == produtoDTO.IdCategoria))
        {
            return BadRequest(new { mensagem = "CATEGORIA NÃO LOCALIZADA!" });
        }

        // Item e produto devem ser cadastrados juntos.
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var item = new Item
            {
                Idcategoria = produtoDTO.IdCategoria,
                Item1 = produtoDTO.Item,
                DescricaoItem = produtoDTO.DescricaoItem,
                Preco = produtoDTO.Preco,
                TipoItem = produtoDTO.TipoItem,
                AtivoProdutoServico = produtoDTO.AtivoProdutoServico
            };

            _context.Items.Add(item);
            await _context.SaveChangesAsync();

            var produto = new Produto
            {
                Iditem = item.Iditem,
                IditemNavigation = item,
                CodigoPreco = produtoDTO.CodigoPreco,
                PrecoVenda = produtoDTO.PrecoVenda,
                Unidade = produtoDTO.Unidade
            };

            _context.Produtos.Add(produto);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return CreatedAtAction(
                nameof(GetProduto),
                new { id = produto.Iditem },
                ExibirProduto(produto));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Erro ao cadastrar item e produto.");

            return StatusCode(500, new { mensagem = "ERRO NO CADASTRO DO PRODUTO." });
        }
    }

    // PUT: api/ProdutoItem/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> PutProduto(int id, ProdutoDTO produtoDTO)
    {
        var produto = await _context.Produtos
            .Include(p => p.IditemNavigation)
            .FirstOrDefaultAsync(p => p.Iditem == id);

        if (produto == null)
        {
            return NotFound(new { mensagem = "PRODUTO NÃO LOCALIZADO!" });
        }

        if (!await _context.Categoria.AnyAsync(c => c.Idcategoria == produtoDTO.IdCategoria))
        {
            return BadRequest(new { mensagem = "CATEGORIA NÃO LOCALIZADA!" });
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var item = produto.IditemNavigation;
            item.Idcategoria = produtoDTO.IdCategoria;
            item.Item1 = produtoDTO.Item;
            item.DescricaoItem = produtoDTO.DescricaoItem;
            item.Preco = produtoDTO.Preco;
            item.TipoItem = produtoDTO.TipoItem;
            item.AtivoProdutoServico = produtoDTO.AtivoProdutoServico;

            produto.CodigoPreco = produtoDTO.CodigoPreco;
            produto.PrecoVenda = produtoDTO.PrecoVenda;
            produto.Unidade = produtoDTO.Unidade;

            // O EF salva as alterações das duas entidades na mesma transação.
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return NoContent();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await transaction.RollbackAsync();
            _logger.LogWarning(ex, "Conflito ao atualizar o produto {Id}.", id);

            if (!await _context.Produtos.AsNoTracking().AnyAsync(p => p.Iditem == id))
            {
                return NotFound(new { mensagem = "PRODUTO NÃO LOCALIZADO!" });
            }

            return Conflict(new { mensagem = "O PRODUTO FOI ALTERADO. CONSULTE OS DADOS E TENTE NOVAMENTE." });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Erro ao atualizar o produto {Id}.", id);

            return StatusCode(500, new { mensagem = "ERRO NA ATUALIZAÇÃO DO PRODUTO." });
        }
    }

    // DELETE: api/ProdutoItem/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteProduto(int id)
    {
        var produto = await _context.Produtos
            .Include(p => p.IditemNavigation)
            .FirstOrDefaultAsync(p => p.Iditem == id);

        if (produto == null)
        {
            return NotFound(new { mensagem = "PRODUTO NÃO LOCALIZADO!" });
        }

        // Preserva o histórico de atendimentos e entradas de estoque.
        if (await _context.Atendimentoitems.AnyAsync(a => a.Iditem == id)
            || await _context.EntradaProdutos.AnyAsync(e => e.Iditem == id))
        {
            return Conflict(new
            {
                mensagem = "O PRODUTO POSSUI ATENDIMENTOS OU ENTRADAS DE ESTOQUE. DESATIVE-O PELO PUT EM VEZ DE EXCLUIR."
            });
        }

        var estoque = await _context.Estoques
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Iditem == id);

        if (estoque != null && estoque.QuantidadeEstoque != 0)
        {
            return Conflict(new { mensagem = "O PRODUTO AINDA POSSUI SALDO EM ESTOQUE." });
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Remove também o estoque vazio que pode ter sido criado por trigger.
            if (estoque != null)
            {
                // Reconfere o saldo no DELETE para não apagar uma entrada concorrente.
                var removidos = await _context.Estoques
                    .Where(e => e.Iditem == id && e.QuantidadeEstoque == 0)
                    .ExecuteDeleteAsync();

                if (removidos != 1)
                {
                    await transaction.RollbackAsync();
                    return Conflict(new { mensagem = "O ESTOQUE FOI ALTERADO. CONSULTE OS DADOS E TENTE NOVAMENTE." });
                }
            }

            _context.Produtos.Remove(produto);
            _context.Items.Remove(produto.IditemNavigation);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return NoContent();
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync();
            _logger.LogWarning(ex, "Conflito ao excluir o produto {Id}.", id);

            return Conflict(new
            {
                mensagem = "NÃO FOI POSSÍVEL EXCLUIR O PRODUTO. CONSULTE SEUS VÍNCULOS E TENTE NOVAMENTE."
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Erro ao excluir o produto {Id}.", id);

            return StatusCode(500, new { mensagem = "ERRO NA EXCLUSÃO DO PRODUTO." });
        }
    }

    // Retorna os dados sem referências circulares entre item e produto.
    private static object ExibirProduto(Produto produto)
    {
        var item = produto.IditemNavigation;

        return new
        {
            idItem = produto.Iditem,
            idCategoria = item.Idcategoria,
            item = item.Item1,
            descricaoItem = item.DescricaoItem,
            preco = item.Preco,
            tipoItem = item.TipoItem,
            ativoProdutoServico = item.AtivoProdutoServico,
            codigoPreco = produto.CodigoPreco,
            precoVenda = produto.PrecoVenda,
            unidade = produto.Unidade
        };
    }
}
