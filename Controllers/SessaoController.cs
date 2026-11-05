using AplicacaoBackEnd.DTO;
using AplicacaoBackEnd.Models;
using AplicacaoBackEnd.Seguranca;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace AplicacaoBackEnd.Controllers;

[ApiController]
[Route("api/Sessao")]
public class SessaoController : ControllerBase
{
    private readonly DbAtendimentoContext _context;
    private readonly IPasswordHasher<Pessoa> _senhas;

    public SessaoController(DbAtendimentoContext context, IPasswordHasher<Pessoa> senhas)
    {
        _context = context;
        _senhas = senhas;
    }

    [HttpPost]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Identificar(IdentificacaoDTO dados)
    {
        var login = dados.Login.Trim().ToLowerInvariant();
        var cpfTexto = Regex.Replace(login, @"[.\-\s]", "");
        var cpfValido = Regex.IsMatch(cpfTexto, "^[0-9]{1,11}$") && long.TryParse(cpfTexto, out _);
        long.TryParse(cpfTexto, out var cpf);
        var pessoa = await _context.Pessoas.AsNoTracking().FirstOrDefaultAsync(p =>
            p.AtivoPessoa == "S" && (cpfValido ? p.Cpf == cpf : p.Email == login));
        if (pessoa == null || string.IsNullOrEmpty(pessoa.SenhaHash))
        {
            return Unauthorized(new { mensagem = "E-mail/CPF ou senha inválidos." });
        }

        PasswordVerificationResult resultado;
        try { resultado = _senhas.VerifyHashedPassword(pessoa, pessoa.SenhaHash, dados.Senha); }
        catch (FormatException) { return Unauthorized(new { mensagem = "E-mail/CPF ou senha inválidos." }); }
        if (resultado == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new { mensagem = "E-mail/CPF ou senha inválidos." });
        }
        if (resultado == PasswordVerificationResult.SuccessRehashNeeded)
        {
            var anterior = pessoa.SenhaHash;
            var atualizado = _senhas.HashPassword(pessoa, dados.Senha);
            // Não sobrescreve uma troca de senha que ocorreu enquanto o login era verificado.
            var alterados = await _context.Pessoas.Where(p => p.Idpessoa == pessoa.Idpessoa && p.SenhaHash == anterior)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.SenhaHash, atualizado));
            if (alterados != 1) return Unauthorized(new { mensagem = "E-mail/CPF ou senha inválidos." });
            pessoa.SenhaHash = atualizado;
        }

        var funcionario = await _context.Funcionarios.AnyAsync(f => f.Idpessoa == pessoa.Idpessoa);
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, pessoa.Idpessoa.ToString()),
            new Claim(ClaimTypes.Name, pessoa.Nome),
            new Claim(ClaimTypes.Role, funcionario ? "Funcionario" : "Cliente"),
            new Claim("senha_versao", Senhas.Marca(pessoa.SenhaHash))
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
        if (pessoa == null) return Unauthorized(new { mensagem = "Faça login novamente." });
        return Ok(new { pessoa.Idpessoa, pessoa.Nome, funcionario = User.IsInRole("Funcionario") });
    }

    [HttpDelete]
    public async Task<IActionResult> Sair()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }
}
