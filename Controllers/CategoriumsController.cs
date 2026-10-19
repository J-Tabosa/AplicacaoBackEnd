using AplicacaoBackEnd.DTO;
using AplicacaoBackEnd.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AplicacaoBackEnd.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CategoriumsController : ControllerBase
{
    private readonly DbAtendimentoContext _context;
    private readonly ILogger<CategoriumsController> _logger;

    public CategoriumsController(DbAtendimentoContext context, ILogger<CategoriumsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetCategoria()
    {
        var categorias = await _context.Categoria
            .AsNoTracking()
            .OrderBy(c => c.Idcategoria)
            .ToListAsync();

        var resposta = new List<CategoriaRespostaDTO>();
        foreach (var categoria in categorias)
        {
            resposta.Add(ExibirCategoria(categoria));
        }

        return Ok(resposta);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetCategorium(int id)
    {
        var categoria = await _context.Categoria.FindAsync(id);
        if (categoria == null)
        {
            return NotFound(new { mensagem = "Categoria não localizada." });
        }

        return Ok(ExibirCategoria(categoria));
    }

    [HttpPost]
    public async Task<IActionResult> PostCategorium(CategoriaDTO dados)
    {
        var categoria = new Categorium
        {
            Categoria = dados.Categoria
        };

        try
        {
            _context.Categoria.Add(categoria);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetCategorium), new { id = categoria.Idcategoria }, ExibirCategoria(categoria));
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Erro ao cadastrar a categoria.");
            return Conflict(new { mensagem = "Não foi possível cadastrar a categoria. Confira os dados." });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> PutCategorium(int id, CategoriaDTO dados)
    {
        var categoria = await _context.Categoria.FindAsync(id);
        if (categoria == null)
        {
            return NotFound(new { mensagem = "Categoria não localizada." });
        }

        try
        {
            categoria.Categoria = dados.Categoria;
            await _context.SaveChangesAsync();
            return NoContent();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Erro ao atualizar a categoria.");
            return Conflict(new { mensagem = "Não foi possível atualizar a categoria. Consulte novamente." });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteCategorium(int id)
    {
        var categoria = await _context.Categoria.FindAsync(id);
        if (categoria == null)
        {
            return NotFound(new { mensagem = "Categoria não localizada." });
        }

        if (await _context.Items.AnyAsync(i => i.Idcategoria == id))
        {
            return Conflict(new { mensagem = "A categoria possui produtos ou serviços vinculados." });
        }

        try
        {
            _context.Categoria.Remove(categoria);
            await _context.SaveChangesAsync();
            return NoContent();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Erro ao excluir a categoria.");
            return Conflict(new { mensagem = "Não foi possível excluir a categoria. Confira seus vínculos." });
        }
    }

    private CategoriaRespostaDTO ExibirCategoria(Categorium categoria)
    {
        return new CategoriaRespostaDTO
        {
            Idcategoria = categoria.Idcategoria,
            Categoria = categoria.Categoria
        };
    }
}
