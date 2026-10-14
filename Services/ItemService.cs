using AplicacaoBackEnd.DTO;
using AplicacaoBackEnd.Models;
using Microsoft.EntityFrameworkCore;

namespace AplicacaoBackEnd.Services;

public class ItemService
{
    private readonly DbAtendimentoContext _context;
    private readonly EstoqueService _estoque;
    public ItemService(DbAtendimentoContext context, EstoqueService estoque)
    {
        _context = context;
        _estoque = estoque;
    }

    public IQueryable<Item> Consulta() => _context.Items.AsNoTracking()
        .Include(i => i.IdcategoriaNavigation).Include(i => i.Produto).ThenInclude(p => p!.Estoque);

    public async Task<Item> ObterAsync(int id) => await Consulta().FirstOrDefaultAsync(i => i.Iditem == id)
        ?? throw new RegraNegocioException(404, "Item não localizado.");

    private async Task<Categorium> CategoriaAsync(int id) => await _context.Categoria.FindAsync(id)
        ?? throw new RegraNegocioException(404, "Categoria não localizada.");

    private static void Aplicar(Item item, ItemDTO dados)
    {
        item.Idcategoria = dados.IdCategoria;
        item.Item1 = dados.Item;
        item.DescricaoItem = dados.DescricaoItem;
        item.Preco = dados.Preco;
        item.TipoItem = dados.TipoItem;
        item.AtivoProdutoServico = dados.AtivoProdutoServico;
    }

    private static void AplicarProduto(Produto produto, ProdutoDadosDTO dados)
    {
        produto.CodigoPreco = dados.CodigoPreco;
        produto.PrecoVenda = dados.PrecoVenda;
        produto.Unidade = dados.Unidade;
    }

    public async Task<Item> CriarAsync(ItemDTO dados)
    {
        var categoria = await CategoriaAsync(dados.IdCategoria);
        var item = new Item { IdcategoriaNavigation = categoria };
        Aplicar(item, dados);
        _context.Items.Add(item);
        await _context.SaveChangesAsync();
        if (dados.TipoItem == "P")
        {
            var produto = new Produto { Iditem = item.Iditem, IditemNavigation = item };
            AplicarProduto(produto, dados.Produto!);
            _context.Produtos.Add(produto);
            // A trigger cria o estoque zerado.
            await _context.SaveChangesAsync();
        }
        return await ObterAsync(item.Iditem);
    }

    private async Task<Item> BloquearAsync(int id)
    {
        var linhas = await _context.Items.FromSqlInterpolated($"SELECT * FROM item WHERE iditem = {id} FOR UPDATE").ToListAsync();
        var item = linhas.SingleOrDefault() ?? throw new RegraNegocioException(404, "Item não localizado.");
        await _context.Entry(item).Reference(i => i.Produto).LoadAsync();
        return item;
    }

    public async Task AtualizarAsync(int id, ItemDTO dados, bool exigirProduto = false)
    {
        var item = await BloquearAsync(id);
        if (exigirProduto && item.Produto == null)
            throw new RegraNegocioException(404, "Produto não localizado.");
        if (item.TipoItem != dados.TipoItem)
            throw new RegraNegocioException(409, "Não é permitido converter produto em serviço ou serviço em produto pelo PUT.");
        item.IdcategoriaNavigation = await CategoriaAsync(dados.IdCategoria);
        Aplicar(item, dados);
        if (dados.TipoItem == "P")
        {
            if (item.Produto == null)
                throw new RegraNegocioException(409, "O item não possui cadastro de produto.");
            AplicarProduto(item.Produto, dados.Produto!);
        }
        await _context.SaveChangesAsync();
    }

    public async Task ExcluirAsync(int id, bool exigirProduto = false)
    {
        var item = await BloquearAsync(id);
        if (exigirProduto && item.Produto == null)
            throw new RegraNegocioException(404, "Produto não localizado.");
        if (await _context.Atendimentoitems.AnyAsync(a => a.Iditem == id)
            || await _context.EntradaProdutos.AnyAsync(e => e.Iditem == id))
            throw new RegraNegocioException(409, "O item possui atendimentos ou entradas de estoque. Desative-o pelo PUT.");
        if (item.Produto != null)
        {
            var saldos = await _estoque.BloquearAsync(new[] { id });
            var saldo = saldos[id];
            if (saldo.QuantidadeEstoque != 0)
                throw new RegraNegocioException(409, "O produto ainda possui saldo em estoque.");
            _context.Estoques.Remove(saldo);
            _context.Produtos.Remove(item.Produto);
        }
        _context.Items.Remove(item);
        await _context.SaveChangesAsync();
    }
}
