using AplicacaoBackEnd.Models;
using Microsoft.EntityFrameworkCore;

namespace AplicacaoBackEnd.Services;

public class EstoqueService
{
    private readonly DbAtendimentoContext _context;
    public EstoqueService(DbAtendimentoContext context) => _context = context;

    // Todas as operações da API bloqueiam os saldos na mesma ordem.
    // Deve ser chamado dentro de uma transação.
    public async Task<Dictionary<int, Estoque>> BloquearAsync(IEnumerable<int> ids)
    {
        var saldos = new Dictionary<int, Estoque>();
        foreach (var id in ids.Distinct().OrderBy(id => id))
        {
            var linhas = await _context.Estoques
                .FromSqlInterpolated($"SELECT * FROM estoque WHERE iditem = {id} FOR UPDATE").ToListAsync();
            var saldo = linhas.SingleOrDefault()
                ?? throw new RegraNegocioException(409, $"O produto {id} não possui estoque. Confira a trigger de cadastro de produto.");
            saldos.Add(id, saldo);
        }
        return saldos;
    }

    public static int Quantidade(decimal quantidade)
    {
        if (quantidade <= 0 || quantidade != decimal.Truncate(quantidade) || quantidade > int.MaxValue)
            throw new RegraNegocioException(400, "Informe uma quantidade positiva em unidades inteiras.");
        return (int)quantidade;
    }

    public static int ConferirSaldo(long quantidade)
    {
        if (quantidade < 0)
            throw new RegraNegocioException(409, "A operação deixaria o estoque negativo. Parte da quantidade já foi utilizada.");
        if (quantidade > int.MaxValue)
            throw new RegraNegocioException(409, "A operação ultrapassa o limite do estoque.");
        return (int)quantidade;
    }

    public async Task ConferirProdutoAsync(int id)
    {
        if (!await _context.Produtos.AnyAsync(p => p.Iditem == id))
            throw new RegraNegocioException(404, $"Produto {id} não localizado.");
    }

    // INSERT continua usando a trigger. Em UPDATE/DELETE, recompõe o custo
    // pela entrada de maior ID restante; sem entradas, mantém o último custo conhecido.
    public async Task RecalcularPrecoAsync(int id)
    {
        var ultima = await _context.EntradaProdutos.AsNoTracking()
            .Where(e => e.Iditem == id).OrderByDescending(e => e.IdentradaProduto).FirstOrDefaultAsync();
        if (ultima == null) return;
        var produto = await _context.Produtos.Include(p => p.IditemNavigation).FirstAsync(p => p.Iditem == id);
        produto.IditemNavigation.Preco = ultima.ValorUnitarioEntrada;
        decimal fator = produto.CodigoPreco switch { 1 => 1.04m, 2 => 1.08m, 3 => 1.10m, _ => 1m };
        produto.PrecoVenda = decimal.Round(ultima.ValorUnitarioEntrada * fator, 2, MidpointRounding.AwayFromZero);
        if (produto.PrecoVenda > 99999999.99m)
            throw new RegraNegocioException(400, "O preço de venda calculado ultrapassa o limite do banco.");
        await _context.SaveChangesAsync();
    }
}
