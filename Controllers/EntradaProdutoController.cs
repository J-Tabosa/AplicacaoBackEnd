using AplicacaoBackEnd.DTO;
using AplicacaoBackEnd.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace AplicacaoBackEnd.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Funcionario")]
public class EntradaProdutoController : ControllerBase
{
    private readonly DbAtendimentoContext _context;
    private readonly ILogger<EntradaProdutoController> _logger;
    private readonly IWebHostEnvironment _ambiente;

    public EntradaProdutoController(DbAtendimentoContext context, ILogger<EntradaProdutoController> logger, IWebHostEnvironment ambiente)
    {
        _context = context;
        _logger = logger;
        _ambiente = ambiente;
    }

    [HttpGet("all")]
    public async Task<IActionResult> GetEntradas()
    {
        var entradas = await _context.EntradaProdutos
            .Include(e => e.IditemNavigation).ThenInclude(p => p.IditemNavigation)
            .Include(e => e.IditemNavigation).ThenInclude(p => p.Estoque)
            .AsNoTracking()
            .OrderBy(e => e.IdentradaProduto)
            .ToListAsync();

        var resposta = new List<object>();
        foreach (var entrada in entradas)
        {
            resposta.Add(ExibirEntrada(entrada));
        }

        return Ok(resposta);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetEntrada(int id)
    {
        var entrada = await _context.EntradaProdutos
            .Include(e => e.IditemNavigation).ThenInclude(p => p.IditemNavigation)
            .Include(e => e.IditemNavigation).ThenInclude(p => p.Estoque)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.IdentradaProduto == id);

        if (entrada == null)
        {
            return NotFound(new { mensagem = "Entrada não localizada." });
        }

        return Ok(ExibirEntrada(entrada));
    }

    [HttpPost]
    public async Task<IActionResult> PostEntrada(EntradaProdutoDTO dados)
    {
        if (dados.DataEntradaProduto == null || dados.DataEntradaProduto.Value.Year < 1000)
        {
            return BadRequest(new { mensagem = "Informe uma data de entrada válida." });
        }

        // O estoque do banco é INT: não pode receber frações.
        if (dados.QuantidadeEntrada <= 0 || dados.QuantidadeEntrada != decimal.Truncate(dados.QuantidadeEntrada))
        {
            return BadRequest(new { mensagem = "Informe uma quantidade positiva em unidades inteiras." });
        }

        await using var transacao = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            if (!await _context.Produtos.AnyAsync(p => p.Iditem == dados.IdItem))
            {
                return NotFound(new { mensagem = "Produto não localizado." });
            }

            var estoques = await BloquearEstoques(new List<int> { dados.IdItem });
            var estoque = estoques.FirstOrDefault();
            if (estoque == null)
            {
                return Conflict(new { mensagem = "Produto sem estoque. Confira a trigger de cadastro." });
            }

            if ((long)estoque.QuantidadeEstoque + dados.QuantidadeEntrada > int.MaxValue)
            {
                return Conflict(new { mensagem = "A entrada ultrapassa o limite do estoque." });
            }

            var entrada = new EntradaProduto
            {
                Iditem = dados.IdItem,
                DataEntradaProduto = dados.DataEntradaProduto.GetValueOrDefault(),
                QuantidadeEntrada = dados.QuantidadeEntrada,
                Unidade = dados.Unidade,
                ValorUnitarioEntrada = dados.ValorUnitarioEntrada
            };

            _context.EntradaProdutos.Add(entrada);
            // A trigger soma o estoque e calcula o preço. Não somar outra vez aqui.
            await _context.SaveChangesAsync();

            var entradaCadastrada = await _context.EntradaProdutos
                .Include(e => e.IditemNavigation).ThenInclude(p => p.IditemNavigation)
                .Include(e => e.IditemNavigation).ThenInclude(p => p.Estoque)
                .AsNoTracking()
                .FirstAsync(e => e.IdentradaProduto == entrada.IdentradaProduto);

            await transacao.CommitAsync();
            return CreatedAtAction(nameof(GetEntrada), new { id = entrada.IdentradaProduto }, ExibirEntrada(entradaCadastrada));
        }
        catch (DbUpdateException ex)
        {
            await transacao.RollbackAsync();
            _logger.LogError(ex, "Erro ao gravar os dados no banco.");

            if (_ambiente.IsDevelopment())
            {
                return Conflict(new
                {
                    mensagem = "Não foi possível registrar a entrada de produto.",
                    detalhe = ex.GetBaseException().Message
                });
            }

            return Conflict(new { mensagem = "Não foi possível gravar. Confira os vínculos e se os dados já foram cadastrados." });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> PutEntrada(int id, EntradaProdutoDTO dados)
    {
        if (dados.DataEntradaProduto == null || dados.DataEntradaProduto.Value.Year < 1000)
        {
            return BadRequest(new { mensagem = "Informe uma data de entrada válida." });
        }

        // O estoque do banco é INT: não pode receber frações.
        if (dados.QuantidadeEntrada <= 0 || dados.QuantidadeEntrada != decimal.Truncate(dados.QuantidadeEntrada))
        {
            return BadRequest(new { mensagem = "Informe uma quantidade positiva em unidades inteiras." });
        }

        await using var transacao = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            var entradas = await _context.EntradaProdutos
                .FromSqlInterpolated($"SELECT * FROM entrada_produto WHERE identrada_produto = {id} FOR UPDATE")
                .ToListAsync();
            var entrada = entradas.FirstOrDefault();

            if (entrada == null)
            {
                return NotFound(new { mensagem = "Entrada não localizada." });
            }

            if (!await _context.Produtos.AnyAsync(p => p.Iditem == dados.IdItem))
            {
                return NotFound(new { mensagem = "Produto não localizado." });
            }

            var idProdutoAnterior = entrada.Iditem;
            var estoques = await BloquearEstoques(new List<int> { idProdutoAnterior, dados.IdItem });

            // Primeiro confere todos os saldos. Só depois altera os registros.
            foreach (var idProduto in new List<int> { idProdutoAnterior, dados.IdItem }.Distinct())
            {
                var estoque = estoques.FirstOrDefault(e => e.Iditem == idProduto);
                if (estoque == null)
                {
                    return Conflict(new { mensagem = "Produto sem estoque. Confira a trigger de cadastro." });
                }

                decimal novoSaldo = estoque.QuantidadeEstoque;
                if (idProduto == idProdutoAnterior)
                {
                    novoSaldo -= entrada.QuantidadeEntrada;
                }

                if (idProduto == dados.IdItem)
                {
                    novoSaldo += dados.QuantidadeEntrada;
                }

                if (novoSaldo < 0 || novoSaldo > int.MaxValue || novoSaldo != decimal.Truncate(novoSaldo))
                {
                    return Conflict(new { mensagem = "A alteração deixaria o estoque negativo, fracionado ou fora do limite." });
                }

                estoque.QuantidadeEstoque = (int)novoSaldo;
            }

            entrada.Iditem = dados.IdItem;
            entrada.DataEntradaProduto = dados.DataEntradaProduto.GetValueOrDefault();
            entrada.QuantidadeEntrada = dados.QuantidadeEntrada;
            entrada.Unidade = dados.Unidade;
            entrada.ValorUnitarioEntrada = dados.ValorUnitarioEntrada;

            // As triggers originais só atuam no INSERT. O PUT ajusta o saldo aqui.
            await _context.SaveChangesAsync();
            foreach (var idProduto in new List<int> { idProdutoAnterior, dados.IdItem }.Distinct())
            {
                var precoValido = await RecalcularPreco(idProduto);
                if (!precoValido)
                {
                    return BadRequest(new { mensagem = "O preço calculado ultrapassa o limite do banco." });
                }
            }

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
    public async Task<IActionResult> DeleteEntrada(int id)
    {
        await using var transacao = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            var entradas = await _context.EntradaProdutos
                .FromSqlInterpolated($"SELECT * FROM entrada_produto WHERE identrada_produto = {id} FOR UPDATE")
                .ToListAsync();
            var entrada = entradas.FirstOrDefault();

            if (entrada == null)
            {
                return NotFound(new { mensagem = "Entrada não localizada." });
            }

            var estoques = await BloquearEstoques(new List<int> { entrada.Iditem });
            var estoque = estoques.FirstOrDefault();
            if (estoque == null)
            {
                return Conflict(new { mensagem = "Produto sem estoque. Confira a trigger de cadastro." });
            }

            var novoSaldo = estoque.QuantidadeEstoque - entrada.QuantidadeEntrada;
            if (novoSaldo < 0 || novoSaldo > int.MaxValue || novoSaldo != decimal.Truncate(novoSaldo))
            {
                return Conflict(new { mensagem = "Não é possível estornar esta entrada. Confira a quantidade já utilizada." });
            }

            estoque.QuantidadeEstoque = (int)novoSaldo;
            _context.EntradaProdutos.Remove(entrada);
            await _context.SaveChangesAsync();

            var precoValido = await RecalcularPreco(entrada.Iditem);
            if (!precoValido)
            {
                return BadRequest(new { mensagem = "O preço calculado ultrapassa o limite do banco." });
            }

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

    // Só use dentro de uma transação. A ordem evita bloqueios cruzados.
    // FOR UPDATE reserva o saldo até o CommitAsync ou o cancelamento.
    private async Task<List<Estoque>> BloquearEstoques(List<int> ids)
    {
        var estoques = new List<Estoque>();
        foreach (var id in ids.Distinct().OrderBy(id => id))
        {
            var registros = await _context.Estoques
                .FromSqlInterpolated($"SELECT * FROM estoque WHERE iditem = {id} FOR UPDATE")
                .ToListAsync();
            estoques.AddRange(registros);
        }

        return estoques;
    }

    // Em PUT/DELETE, usa o custo da entrada de maior ID que ainda existe.
    private async Task<bool> RecalcularPreco(int idProduto)
    {
        var ultimaEntrada = await _context.EntradaProdutos
            .AsNoTracking()
            .Where(e => e.Iditem == idProduto)
            .OrderByDescending(e => e.IdentradaProduto)
            .FirstOrDefaultAsync();

        if (ultimaEntrada == null)
        {
            return true; // Sem outra entrada, mantém o último custo conhecido.
        }

        var produto = await _context.Produtos
            .Include(p => p.IditemNavigation)
            .FirstAsync(p => p.Iditem == idProduto);

        decimal fator = 1;
        if (produto.CodigoPreco == 1)
        {
            fator = 1.04m;
        }
        else if (produto.CodigoPreco == 2)
        {
            fator = 1.08m;
        }
        else if (produto.CodigoPreco == 3)
        {
            fator = 1.10m;
        }

        var precoVenda = decimal.Round(ultimaEntrada.ValorUnitarioEntrada * fator, 2, MidpointRounding.AwayFromZero);
        if (precoVenda > 99999999.99m)
        {
            return false;
        }

        produto.IditemNavigation.Preco = ultimaEntrada.ValorUnitarioEntrada;
        produto.PrecoVenda = precoVenda;
        await _context.SaveChangesAsync();
        return true;
    }

    private object ExibirEntrada(EntradaProduto entrada)
    {
        return new
        {
            IdEntradaProduto = entrada.IdentradaProduto,
            IdItem = entrada.Iditem,
            Produto = entrada.IditemNavigation.IditemNavigation.Item1,
            entrada.DataEntradaProduto,
            entrada.QuantidadeEntrada,
            entrada.Unidade,
            entrada.ValorUnitarioEntrada,
            QuantidadeEstoque = entrada.IditemNavigation.Estoque?.QuantidadeEstoque ?? 0,
            PrecoVenda = entrada.IditemNavigation.PrecoVenda
        };
    }
}
