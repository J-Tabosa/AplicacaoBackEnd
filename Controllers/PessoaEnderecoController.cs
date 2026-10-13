using AplicacaoBackEnd.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AplicacaoBackEnd.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PessoaEnderecoController : ControllerBase
{
    private readonly DbAtendimentoContext _context;

    public PessoaEnderecoController(DbAtendimentoContext context)
    {
        _context = context;
    }

    // GET: api/PessoaEndereco/all
    [HttpGet("all")]
    public async Task<IActionResult> GetPessoasEnderecos()
    {
        var pessoas = await _context.Pessoas
            .AsNoTracking()
            .OrderBy(p => p.Idpessoa)
            .Select(p => new
            {
                p.Idpessoa,
                p.Nome,
                p.Cpf,
                p.DataNascimento,
                p.Sexo,
                p.Email,
                p.Telefone,
                p.AtivoPessoa,
                Enderecos = p.Enderecos
                    .OrderBy(e => e.Idendereco)
                    .Select(e => new
                    {
                        e.Idendereco,
                        e.Cep,
                        e.Numero,
                        e.Complemento
                    }).ToList()
            })
            .ToListAsync();

        return Ok(pessoas);
    }

    // GET: api/PessoaEndereco/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetPessoaEndereco(int id)
    {
        // Uma pessoa pode ter vários endereços. A projeção evita ciclos no JSON.
        var pessoa = await _context.Pessoas
            .AsNoTracking()
            .Where(p => p.Idpessoa == id)
            .Select(p => new
            {
                p.Idpessoa,
                p.Nome,
                p.Cpf,
                p.DataNascimento,
                p.Sexo,
                p.Email,
                p.Telefone,
                p.AtivoPessoa,
                Enderecos = p.Enderecos
                    .OrderBy(e => e.Idendereco)
                    .Select(e => new
                    {
                        e.Idendereco,
                        e.Cep,
                        e.Numero,
                        e.Complemento
                    }).ToList()
            })
            .FirstOrDefaultAsync();

        if (pessoa == null)
        {
            return NotFound(new { mensagem = "PESSOA NÃO LOCALIZADA!" });
        }

        return Ok(pessoa);
    }
}
