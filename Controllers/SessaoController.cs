using AplicacaoBackEnd.DTO;
using AplicacaoBackEnd.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AplicacaoBackEnd.Controllers;

[ApiController]
[Route("api/Sessao")]
public class SessaoController : ControllerBase
{
    private readonly DbAtendimentoContext _context;

    public SessaoController(DbAtendimentoContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> Identificar(IdentificacaoDTO dados)
    {
        var pessoa = await _context.Pessoas.AsNoTracking().FirstOrDefaultAsync(p =>
            p.Cpf == dados.Cpf && p.DataNascimento == dados.DataNascimento && p.AtivoPessoa == "S");
        if (pessoa == null)
        {
            return Unauthorized(new { mensagem = "CPF ou data de nascimento inválidos, ou cadastro inativo." });
        }

        var funcionario = await _context.Funcionarios.AnyAsync(f => f.Idpessoa == pessoa.Idpessoa);
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, pessoa.Idpessoa.ToString()),
            new Claim(ClaimTypes.Name, pessoa.Nome),
            new Claim(ClaimTypes.Role, funcionario ? "Funcionario" : "Cliente")
        };
        var identidade = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identidade), new AuthenticationProperties { IsPersistent = false });
        return Ok(new { pessoa.Idpessoa, pessoa.Nome, funcionario });
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Consultar()
    {
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id);
        var pessoa = await _context.Pessoas.AsNoTracking().FirstOrDefaultAsync(p => p.Idpessoa == id);
        if (pessoa == null)
        {
            return Unauthorized(new { mensagem = "Identifique-se novamente." });
        }
        return Ok(new { pessoa.Idpessoa, pessoa.Nome, funcionario = User.IsInRole("Funcionario") });
    }

    [HttpDelete]
    public async Task<IActionResult> Sair()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }
}
