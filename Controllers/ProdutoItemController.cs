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
