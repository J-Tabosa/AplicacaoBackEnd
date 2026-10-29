using AplicacaoBackEnd.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AplicacaoBackEnd.Controllers;

[ApiController]
[Route("api/Estoque")]
[Authorize(Roles = "Funcionario")]
public class EstoqueController : ControllerBase
{
    private readonly DbAtendimentoContext _context;
    public EstoqueController(DbAtendimentoContext context) { _context = context; }

    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        return Ok(await _context.Estoques.AsNoTracking().OrderBy(e => e.Iditem).Select(e => new
        {
            IdItem = e.Iditem,
            Item = e.IditemNavigation.IditemNavigation.Item1,
            DescricaoItem = e.IditemNavigation.IditemNavigation.DescricaoItem,
            Preco = e.IditemNavigation.IditemNavigation.Preco,
            e.IditemNavigation.PrecoVenda,
            e.IditemNavigation.Unidade,
            Categoria = e.IditemNavigation.IditemNavigation.IdcategoriaNavigation.Categoria,
            e.QuantidadeEstoque,
            AtivoProdutoServico = e.IditemNavigation.IditemNavigation.AtivoProdutoServico
        }).ToListAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Consultar(int id)
    {
        var estoque = await _context.Estoques.AsNoTracking().Where(e => e.Iditem == id).Select(e => new
        {
            IdItem = e.Iditem,
            Item = e.IditemNavigation.IditemNavigation.Item1,
            DescricaoItem = e.IditemNavigation.IditemNavigation.DescricaoItem,
            Preco = e.IditemNavigation.IditemNavigation.Preco,
            e.IditemNavigation.PrecoVenda,
            e.IditemNavigation.Unidade,
            Categoria = e.IditemNavigation.IditemNavigation.IdcategoriaNavigation.Categoria,
            e.QuantidadeEstoque,
            AtivoProdutoServico = e.IditemNavigation.IditemNavigation.AtivoProdutoServico
        }).FirstOrDefaultAsync();
        if (estoque == null) { return NotFound(new { mensagem = "Produto não localizado no estoque." }); }
        return Ok(estoque);
    }
}
