using AplicacaoBackEnd.DTO;
using AplicacaoBackEnd.Models;
using Microsoft.EntityFrameworkCore;

namespace AplicacaoBackEnd.Services;

public class AtendimentoService
{
    private readonly DbAtendimentoContext _context;
    private readonly EstoqueService _estoque;
    public AtendimentoService(DbAtendimentoContext context, EstoqueService estoque)
    {
        _context = context;
        _estoque = estoque;
    }

    public IQueryable<Atendimento> Consulta() => _context.Atendimentos.AsNoTracking()
        .Include(a => a.IdpessoaNavigation).ThenInclude(p => p.Enderecos)
        .Include(a => a.Atendimentoitems).ThenInclude(i => i.IditemNavigation)
        .Include(a => a.Atendimentoitems).ThenInclude(i => i.IdfuncionarioNavigation).ThenInclude(f => f.IdpessoaNavigation);

    public async Task<AtendimentoRespostaDTO> ObterRespostaAsync(int id)
    {
        var atendimento = await Consulta().FirstOrDefaultAsync(a => a.Idatendimento == id)
            ?? throw new RegraNegocioException(404, "Atendimento não localizado.");
        return ApiMapeamento.Atendimento(atendimento);
    }

    public async Task<Atendimento> BloquearAsync(int id)
    {
        var linhas = await _context.Atendimentos
            .FromSqlInterpolated($"SELECT * FROM atendimento WHERE idatendimento = {id} FOR UPDATE").ToListAsync();
        var atendimento = linhas.SingleOrDefault() ?? throw new RegraNegocioException(404, "Atendimento não localizado.");
        await _context.Entry(atendimento).Collection(a => a.Atendimentoitems).LoadAsync();
        return atendimento;
    }

    public async Task ConferirClienteAsync(int id)
    {
        if (!await _context.Pessoas.AnyAsync(p => p.Idpessoa == id))
            throw new RegraNegocioException(404, "Pessoa atendida não localizada.");
    }

    public static List<AtendimentoItemDTO> ExtrairItens(Atendimento atendimento) => atendimento.Atendimentoitems
        .Select(i => new AtendimentoItemDTO
        {
            IdItem = i.Iditem, IdFuncionario = i.Idfuncionario,
            QuantidadeItem = i.QuantidadeItem, ValorUnitarioEntrada = i.ValorunitarioEntrada ?? 0
        }).ToList();

    // Todos os caminhos (PUT completo e endpoints de itens) usam a mesma regra.
    // As triggers recebidas só atuam no INSERT. UPDATE/DELETE são compensados aqui.
    public async Task SincronizarItensAsync(Atendimento atendimento, List<AtendimentoItemDTO> dados, bool permitirVazio = false)
    {
        if (dados == null || (!permitirVazio && dados.Count == 0) || dados.Any(i => i == null))
            throw new RegraNegocioException(400, "Informe pelo menos um item válido no atendimento.");
        if (dados.Any(i => i.IdItem <= 0 || i.IdFuncionario <= 0 || i.QuantidadeItem <= 0
            || !i.ValorUnitarioEntrada.HasValue || i.ValorUnitarioEntrada < 0 || i.ValorUnitarioEntrada > 99999999.99m))
            throw new RegraNegocioException(400, "Confira os identificadores, as quantidades positivas e os valores dos itens.");
        if (dados.Select(i => i.IdItem).Distinct().Count() != dados.Count)
            throw new RegraNegocioException(400, "Cada item só pode aparecer uma vez por atendimento. Some as quantidades.");

        var anteriores = atendimento.Atendimentoitems.ToDictionary(i => i.Iditem);
        var novos = dados.ToDictionary(i => i.IdItem);
        var ids = anteriores.Keys.Union(novos.Keys).ToArray();
        var itens = await _context.Items.AsNoTracking().Include(i => i.Produto)
            .Where(i => ids.Contains(i.Iditem)).ToDictionaryAsync(i => i.Iditem);
        if (ids.Any(id => !itens.ContainsKey(id)))
            throw new RegraNegocioException(404, "Um dos produtos ou serviços não existe.");
        if (novos.Keys.Any(id => itens[id].TipoItem == "P" && itens[id].Produto == null))
            throw new RegraNegocioException(409, "Um dos itens do tipo P ainda não possui cadastro de produto.");
        var funcionarios = dados.Select(i => i.IdFuncionario).Distinct().ToArray();
        var encontrados = await _context.Funcionarios.Where(f => funcionarios.Contains(f.Idpessoa)).Select(f => f.Idpessoa).ToListAsync();
        if (funcionarios.Any(id => !encontrados.Contains(id)))
            throw new RegraNegocioException(404, "Um dos funcionários não existe.");

        // Usa a existência da tabela produto, como a própria trigger fornecida.
        var produtos = ids.Where(id => itens[id].Produto != null).ToArray();
        var saldos = await _estoque.BloquearAsync(produtos);
        foreach (var id in produtos)
        {
            var antes = anteriores.TryGetValue(id, out var anterior) ? anterior.QuantidadeItem : 0;
            var depois = novos.TryGetValue(id, out var novo) ? novo.QuantidadeItem : 0;
            long final = (long)saldos[id].QuantidadeEstoque + antes - depois;
            if (final < 0)
                throw new RegraNegocioException(409, $"Estoque insuficiente para o produto {id}.");
            EstoqueService.ConferirSaldo(final);
        }

        foreach (var anterior in anteriores.Values)
        {
            if (novos.TryGetValue(anterior.Iditem, out var novo))
            {
                if (saldos.TryGetValue(anterior.Iditem, out var saldo))
                    saldo.QuantidadeEstoque = EstoqueService.ConferirSaldo((long)saldo.QuantidadeEstoque + anterior.QuantidadeItem - novo.QuantidadeItem);
                anterior.Idfuncionario = novo.IdFuncionario;
                anterior.QuantidadeItem = novo.QuantidadeItem;
                anterior.ValorunitarioEntrada = novo.ValorUnitarioEntrada;
            }
            else
            {
                if (saldos.TryGetValue(anterior.Iditem, out var saldo))
                    saldo.QuantidadeEstoque = EstoqueService.ConferirSaldo((long)saldo.QuantidadeEstoque + anterior.QuantidadeItem);
                _context.Atendimentoitems.Remove(anterior);
            }
        }
        // Primeiro salva as compensações e as alterações dos vínculos existentes.
        await _context.SaveChangesAsync();

        foreach (var novo in dados.Where(i => !anteriores.ContainsKey(i.IdItem)))
        {
            _context.Atendimentoitems.Add(new Atendimentoitem
            {
                Idatendimento = atendimento.Idatendimento, Iditem = novo.IdItem,
                Idfuncionario = novo.IdFuncionario, QuantidadeItem = novo.QuantidadeItem,
                ValorunitarioEntrada = novo.ValorUnitarioEntrada
            });
        }
        // Só os vínculos novos disparam a baixa automática da trigger.
        await _context.SaveChangesAsync();
    }
}
