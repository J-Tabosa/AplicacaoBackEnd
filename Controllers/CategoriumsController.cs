using AplicacaoBackEnd.DTO;
using AplicacaoBackEnd.Models;
using AplicacaoBackEnd.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AplicacaoBackEnd.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriumsController : ControllerBase
{
    private readonly DbAtendimentoContext _context;
    public CategoriumsController(DbAtendimentoContext context) => _context = context;
    private static CategoriaRespostaDTO Resposta(Categorium categoria) => new() { Idcategoria = categoria.Idcategoria, Categoria = categoria.Categoria };

    [HttpGet]
    public async Task<ActionResult<List<CategoriaRespostaDTO>>> GetCategoria()
    {
        var categorias = await _context.Categoria.AsNoTracking().OrderBy(c => c.Idcategoria).ToListAsync();
        return Ok(categorias.Select(Resposta).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CategoriaRespostaDTO>> GetCategorium(int id)
    {
        var categoria = await _context.Categoria.FindAsync(id) ?? throw new RegraNegocioException(404, "Categoria não localizada.");
        return Ok(Resposta(categoria));
    }

    [HttpPost]
    public async Task<ActionResult<CategoriaRespostaDTO>> PostCategorium(CategoriaDTO dados)
    {
        var categoria = new Categorium { Categoria = dados.Categoria };
        _context.Categoria.Add(categoria);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetCategorium), new { id = categoria.Idcategoria }, Resposta(categoria));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> PutCategorium(int id, CategoriaDTO dados)
    {
        var categoria = await _context.Categoria.FindAsync(id) ?? throw new RegraNegocioException(404, "Categoria não localizada.");
        categoria.Categoria = dados.Categoria;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteCategorium(int id)
    {
        var categoria = await _context.Categoria.FindAsync(id) ?? throw new RegraNegocioException(404, "Categoria não localizada.");
        if (await _context.Items.AnyAsync(i => i.Idcategoria == id))
            throw new RegraNegocioException(409, "A categoria possui produtos ou serviços vinculados.");
        _context.Categoria.Remove(categoria);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
