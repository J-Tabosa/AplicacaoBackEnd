using AplicacaoBackEnd.DTO;
using AplicacaoBackEnd.Models;
using AplicacaoBackEnd.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace AplicacaoBackEnd.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EntradaProdutoController : ControllerBase
{
    private readonly DbAtendimentoContext _context;
    private readonly EstoqueService _estoque;
    public EntradaProdutoController(DbAtendimentoContext context, EstoqueService estoque)
    {
        _context = context;
        _estoque = estoque;
    }

    private IQueryable<EntradaProduto> Consulta() => _context.EntradaProdutos.AsNoTracking()
        .Include(e => e.IditemNavigation).ThenInclude(p => p.IditemNavigation)
        .Include(e => e.IditemNavigation).ThenInclude(p => p.Estoque);

    [HttpGet("all")]
    public async Task<ActionResult<List<EntradaProdutoRespostaDTO>>> GetEntradas()
    {
        var entradas = await Consulta().OrderBy(e => e.IdentradaProduto).ToListAsync();
        return Ok(entradas.Select(ApiMapeamento.Entrada).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EntradaProdutoRespostaDTO>> GetEntrada(int id)
    {
        var entrada = await Consulta().FirstOrDefaultAsync(e => e.IdentradaProduto == id)
            ?? throw new RegraNegocioException(404, "Entrada não localizada.");
        return Ok(ApiMapeamento.Entrada(entrada));
    }

    [HttpPost]
    public async Task<ActionResult<EntradaProdutoRespostaDTO>> PostEntrada(EntradaProdutoDTO dados)
    {
        var data = Validacao.Data(dados.DataEntradaProduto, "entrada");
        var quantidade = EstoqueService.Quantidade(dados.QuantidadeEntrada);
        await using var transacao = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        await _estoque.ConferirProdutoAsync(dados.IdItem);
        var saldos = await _estoque.BloquearAsync(new[] { dados.IdItem });
        EstoqueService.ConferirSaldo((long)saldos[dados.IdItem].QuantidadeEstoque + quantidade);
        var entrada = new EntradaProduto
        {
            Iditem = dados.IdItem, DataEntradaProduto = data, QuantidadeEntrada = quantidade,
            Unidade = dados.Unidade, ValorUnitarioEntrada = dados.ValorUnitarioEntrada
        };
        _context.EntradaProdutos.Add(entrada);
        // A trigger soma o estoque e atualiza os preços. Não somar novamente aqui.
        await _context.SaveChangesAsync();
        var resposta = ApiMapeamento.Entrada(await Consulta().FirstAsync(e => e.IdentradaProduto == entrada.IdentradaProduto));
        await transacao.CommitAsync();
        return CreatedAtAction(nameof(GetEntrada), new { id = entrada.IdentradaProduto }, resposta);
    }

    private async Task<EntradaProduto> BloquearEntrada(int id)
    {
        var linhas = await _context.EntradaProdutos
            .FromSqlInterpolated($"SELECT * FROM entrada_produto WHERE identrada_produto = {id} FOR UPDATE").ToListAsync();
        return linhas.SingleOrDefault() ?? throw new RegraNegocioException(404, "Entrada não localizada.");
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> PutEntrada(int id, EntradaProdutoDTO dados)
    {
        var data = Validacao.Data(dados.DataEntradaProduto, "entrada");
        var quantidade = EstoqueService.Quantidade(dados.QuantidadeEntrada);
        await using var transacao = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        var entrada = await BloquearEntrada(id);
        await _estoque.ConferirProdutoAsync(dados.IdItem);
        var anterior = entrada.Iditem;
        var quantidadeAnterior = EstoqueService.Quantidade(entrada.QuantidadeEntrada);
        var saldos = await _estoque.BloquearAsync(new[] { anterior, dados.IdItem });
        if (anterior == dados.IdItem)
            saldos[anterior].QuantidadeEstoque = EstoqueService.ConferirSaldo(
                (long)saldos[anterior].QuantidadeEstoque - quantidadeAnterior + quantidade);
        else
        {
            saldos[anterior].QuantidadeEstoque = EstoqueService.ConferirSaldo((long)saldos[anterior].QuantidadeEstoque - quantidadeAnterior);
            saldos[dados.IdItem].QuantidadeEstoque = EstoqueService.ConferirSaldo((long)saldos[dados.IdItem].QuantidadeEstoque + quantidade);
        }
        entrada.Iditem = dados.IdItem;
        entrada.DataEntradaProduto = data;
        entrada.QuantidadeEntrada = quantidade;
        entrada.Unidade = dados.Unidade;
        entrada.ValorUnitarioEntrada = dados.ValorUnitarioEntrada;
        await _context.SaveChangesAsync();
        foreach (var produto in new[] { anterior, dados.IdItem }.Distinct())
            await _estoque.RecalcularPrecoAsync(produto);
        await transacao.CommitAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteEntrada(int id)
    {
        await using var transacao = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        var entrada = await BloquearEntrada(id);
        var saldos = await _estoque.BloquearAsync(new[] { entrada.Iditem });
        saldos[entrada.Iditem].QuantidadeEstoque = EstoqueService.ConferirSaldo(
            (long)saldos[entrada.Iditem].QuantidadeEstoque - EstoqueService.Quantidade(entrada.QuantidadeEntrada));
        _context.EntradaProdutos.Remove(entrada);
        await _context.SaveChangesAsync();
        await _estoque.RecalcularPrecoAsync(entrada.Iditem);
        await transacao.CommitAsync();
        return NoContent();
    }
}
