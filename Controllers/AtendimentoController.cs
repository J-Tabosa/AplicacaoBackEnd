using AplicacaoBackEnd.DTO;
using AplicacaoBackEnd.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Security.Claims;

namespace AplicacaoBackEnd.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AtendimentoController : ControllerBase
{
    private readonly DbAtendimentoContext _context;
    private readonly ILogger<AtendimentoController> _logger;

    public AtendimentoController(DbAtendimentoContext context, ILogger<AtendimentoController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [Authorize(Roles = "Funcionario")]
    [HttpGet("all")]
    public async Task<IActionResult> GetAtendimentos(string? status = null)
    {
        var consulta = ConsultarAtendimentos();
        if (!string.IsNullOrWhiteSpace(status))
        {
            var codigo = NormalizarStatus(status);
            if (codigo == null) { return BadRequest(new { mensagem = "Situação inválida." }); }
            consulta = consulta.Where(a => a.StatusAtendimento == codigo);
        }
        var atendimentos = await consulta
            .OrderBy(a => a.Idatendimento)
            .ToListAsync();

        var resposta = new List<object>();
        foreach (var atendimento in atendimentos)
        {
            resposta.Add(ExibirAtendimento(atendimento));
        }

        return Ok(resposta);
    }

    [Authorize(Roles = "Funcionario")]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetAtendimento(int id)
    {
        var atendimento = await ConsultarAtendimentos()
            .FirstOrDefaultAsync(a => a.Idatendimento == id);

        if (atendimento == null)
        {
            return NotFound(new { mensagem = "Atendimento não localizado." });
        }

        return Ok(ExibirAtendimento(atendimento));
    }

    [Authorize(Roles = "Funcionario")]
    [HttpPost]
    public async Task<IActionResult> PostAtendimento(AtendimentoDTO dados)
    {
        if (dados.StatusAtendimento != "A")
        {
            return BadRequest(new { mensagem = "Novos pedidos devem começar em aberto (A)." });
        }
        if (dados.DataAtendimento == null || dados.DataAtendimento.Value.Year < 1000)
        {
            return BadRequest(new { mensagem = "Informe uma data de atendimento válida." });
        }

        await using var transacao = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            if (!await _context.Pessoas.AnyAsync(p => p.Idpessoa == dados.IdPessoa))
            {
                return NotFound(new { mensagem = "Pessoa atendida não localizada." });
            }

            var atendimento = new Atendimento
            {
                Idpessoa = dados.IdPessoa,
                DataAtendimento = dados.DataAtendimento.GetValueOrDefault(),
                StatusAtendimento = dados.StatusAtendimento
            };

            _context.Atendimentos.Add(atendimento);
            await _context.SaveChangesAsync();

            // O método abaixo está neste controller e também é usado no PUT.
            var erro = await AtualizarItens(atendimento, dados.Itens);
            if (erro != null)
            {
                return erro; // A transação sem CommitAsync desfaz o atendimento.
            }

            var atendimentoCadastrado = await ConsultarAtendimentos()
                .FirstAsync(a => a.Idatendimento == atendimento.Idatendimento);

            await transacao.CommitAsync();
            return CreatedAtAction(nameof(GetAtendimento), new { id = atendimento.Idatendimento }, ExibirAtendimento(atendimentoCadastrado));
        }
        catch (DbUpdateException ex)
        {
            await transacao.RollbackAsync();
            _logger.LogError(ex, "Erro ao gravar os dados no banco.");
            return Conflict(new { mensagem = "Não foi possível gravar. Confira os vínculos e se os dados já foram cadastrados." });
        }
    }

    // A lista enviada no PUT representa todos os itens que devem permanecer.
    [Authorize(Roles = "Funcionario")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> PutAtendimento(int id, AtendimentoDTO dados)
    {
        if (dados.DataAtendimento == null || dados.DataAtendimento.Value.Year < 1000)
        {
            return BadRequest(new { mensagem = "Informe uma data de atendimento válida." });
        }

        await using var transacao = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            var atendimento = await BuscarAtendimentoParaAlterar(id);
            if (atendimento == null)
            {
                return NotFound(new { mensagem = "Atendimento não localizado." });
            }

            if (atendimento.StatusAtendimento != "A" && atendimento.StatusAtendimento != null)
            {
                return Conflict(new { mensagem = "Somente pedidos em aberto podem ter seus itens alterados." });
            }

            if (!await _context.Pessoas.AnyAsync(p => p.Idpessoa == dados.IdPessoa))
            {
                return NotFound(new { mensagem = "Pessoa atendida não localizada." });
            }

            if (dados.StatusAtendimento != "A")
            {
                return Conflict(new { mensagem = "Use a operação de situação do pedido para avançar ou cancelar." });
            }

            atendimento.Idpessoa = dados.IdPessoa;
            atendimento.DataAtendimento = dados.DataAtendimento.GetValueOrDefault();
            atendimento.StatusAtendimento = dados.StatusAtendimento;

            var erro = await AtualizarItens(atendimento, dados.Itens);
            if (erro != null)
            {
                return erro;
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

    [Authorize(Roles = "Funcionario")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAtendimento(int id)
    {
        await using var transacao = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            var atendimento = await BuscarAtendimentoParaAlterar(id);
            if (atendimento == null)
            {
                return NotFound(new { mensagem = "Atendimento não localizado." });
            }

            if (atendimento.StatusAtendimento == "F" || atendimento.StatusAtendimento == "P")
            {
                return Conflict(new { mensagem = "Pedidos em processamento ou concluídos não podem ser apagados. Use o fluxo de situação." });
            }
            if (atendimento.StatusAtendimento == "C")
            {
                _context.Atendimentoitems.RemoveRange(atendimento.Atendimentoitems);
            }
            else
            {
                var erro = await AtualizarItens(atendimento, new List<AtendimentoItemDTO>(), true);
                if (erro != null) { return erro; }
            }

            _context.Atendimentos.Remove(atendimento);
            await _context.SaveChangesAsync();
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

    [Authorize(Roles = "Funcionario")]
    [HttpPost("{id:int}/itens")]
    public async Task<IActionResult> PostItem(int id, AtendimentoItemDTO dados)
    {
        await using var transacao = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            var atendimento = await BuscarAtendimentoParaAlterar(id);
            if (atendimento == null)
            {
                return NotFound(new { mensagem = "Atendimento não localizado." });
            }

            if (atendimento.StatusAtendimento != "A" && atendimento.StatusAtendimento != null)
            {
                return Conflict(new { mensagem = "Somente pedidos em aberto podem ter seus itens alterados." });
            }

            var itens = CopiarItens(atendimento);
            if (itens.Any(i => i.IdItem == dados.IdItem))
            {
                return Conflict(new { mensagem = "O item já pertence ao atendimento. Use o PUT para alterar a quantidade." });
            }

            itens.Add(dados);
            var erro = await AtualizarItens(atendimento, itens);
            if (erro != null)
            {
                return erro;
            }

            var atendimentoAtualizado = await ConsultarAtendimentos().FirstAsync(a => a.Idatendimento == id);
            await transacao.CommitAsync();
            return CreatedAtAction(nameof(GetAtendimento), new { id }, ExibirAtendimento(atendimentoAtualizado));
        }
        catch (DbUpdateException ex)
        {
            await transacao.RollbackAsync();
            _logger.LogError(ex, "Erro ao gravar os dados no banco.");
            return Conflict(new { mensagem = "Não foi possível gravar. Confira os vínculos e se os dados já foram cadastrados." });
        }
    }

    [Authorize(Roles = "Funcionario")]
    [HttpPut("{id:int}/itens/{idItem:int}")]
    public async Task<IActionResult> PutItem(int id, int idItem, AtendimentoItemDTO dados)
    {
        if (idItem != dados.IdItem)
        {
            return BadRequest(new { mensagem = "O ID do item da URL deve corresponder ao JSON." });
        }

        await using var transacao = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            var atendimento = await BuscarAtendimentoParaAlterar(id);
            if (atendimento == null)
            {
                return NotFound(new { mensagem = "Atendimento não localizado." });
            }

            if (atendimento.StatusAtendimento != "A" && atendimento.StatusAtendimento != null)
            {
                return Conflict(new { mensagem = "Somente pedidos em aberto podem ter seus itens alterados." });
            }

            var itens = CopiarItens(atendimento);
            var item = itens.FirstOrDefault(i => i.IdItem == idItem);
            if (item == null)
            {
                return NotFound(new { mensagem = "Item não localizado neste atendimento." });
            }

            item.IdFuncionario = dados.IdFuncionario;
            item.QuantidadeItem = dados.QuantidadeItem;
            item.ValorUnitarioEntrada = dados.ValorUnitarioEntrada;

            var erro = await AtualizarItens(atendimento, itens);
            if (erro != null)
            {
                return erro;
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

    [Authorize(Roles = "Funcionario")]
    [HttpDelete("{id:int}/itens/{idItem:int}")]
    public async Task<IActionResult> DeleteItem(int id, int idItem)
    {
        await using var transacao = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            var atendimento = await BuscarAtendimentoParaAlterar(id);
            if (atendimento == null)
            {
                return NotFound(new { mensagem = "Atendimento não localizado." });
            }

            if (atendimento.StatusAtendimento != "A" && atendimento.StatusAtendimento != null)
            {
                return Conflict(new { mensagem = "Somente pedidos em aberto podem ter seus itens alterados." });
            }

            var itens = CopiarItens(atendimento);
            var item = itens.FirstOrDefault(i => i.IdItem == idItem);
            if (item == null)
            {
                return NotFound(new { mensagem = "Item não localizado neste atendimento." });
            }

            if (itens.Count == 1)
            {
                return Conflict(new { mensagem = "O atendimento deve ter pelo menos um item. Para remover tudo, exclua o atendimento." });
            }

            itens.Remove(item);
            var erro = await AtualizarItens(atendimento, itens);
            if (erro != null)
            {
                return erro;
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


    [Authorize]
    [HttpGet("meus")]
    public async Task<IActionResult> MeusPedidos()
    {
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var idCliente);
        var pedidos = await ConsultarAtendimentos().Where(a => a.Idpessoa == idCliente)
            .OrderByDescending(a => a.Idatendimento).ToListAsync();
        return Ok(pedidos.Select(ExibirAtendimento));
    }

    [Authorize]
    [HttpGet("meus/{id:int}")]
    public async Task<IActionResult> MeuPedido(int id)
    {
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var idCliente);
        var pedido = await ConsultarAtendimentos()
            .FirstOrDefaultAsync(a => a.Idatendimento == id && a.Idpessoa == idCliente);
        if (pedido == null) { return NotFound(new { mensagem = "Pedido não localizado para este cliente." }); }
        return Ok(ExibirAtendimento(pedido));
    }

    [Authorize]
    [HttpPost("checkout")]
    public async Task<IActionResult> ConfirmarCompra(CompraDTO dados)
    {
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var idCliente);
        if (dados.Itens.Any(i => i == null) || dados.Itens.Select(i => i.IdItem).Distinct().Count() != dados.Itens.Count)
        {
            return BadRequest(new { mensagem = "Não repita itens no carrinho." });
        }
        await using var transacao = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            var funcionario = await _context.Funcionarios.AsNoTracking()
                .Where(f => f.IdpessoaNavigation.AtivoPessoa == "S").OrderBy(f => f.Idpessoa).FirstOrDefaultAsync();
            if (funcionario == null)
            {
                return Conflict(new { mensagem = "A loja ainda não possui funcionário responsável para receber pedidos." });
            }
            var itens = new List<AtendimentoItemDTO>();
            foreach (var linha in dados.Itens)
            {
                var item = await _context.Items.AsNoTracking().Include(i => i.Produto)
                    .FirstOrDefaultAsync(i => i.Iditem == linha.IdItem && i.AtivoProdutoServico == "S");
                if (item == null) { return Conflict(new { mensagem = "Um dos itens não está mais disponível. Atualize seu carrinho." }); }
                if (item.TipoItem == "P" && (item.Produto == null || item.Produto.PrecoVenda == null))
                {
                    return Conflict(new { mensagem = "Produto sem preço de venda disponível." });
                }
                decimal preco = item.TipoItem == "P" ? item.Produto!.PrecoVenda!.Value : item.Preco;
                itens.Add(new AtendimentoItemDTO
                {
                    IdItem = item.Iditem,
                    IdFuncionario = funcionario.Idpessoa,
                    QuantidadeItem = linha.QuantidadeItem,
                    ValorUnitarioEntrada = preco
                });
            }
            var pedido = new Atendimento
            {
                Idpessoa = idCliente,
                DataAtendimento = DateOnly.FromDateTime(DateTime.Today),
                StatusAtendimento = "A"
            };
            _context.Atendimentos.Add(pedido);
            await _context.SaveChangesAsync();
            var erro = await AtualizarItens(pedido, itens);
            if (erro != null) { return erro; }
            var resposta = await ConsultarAtendimentos().FirstAsync(a => a.Idatendimento == pedido.Idatendimento);
            await transacao.CommitAsync();
            return CreatedAtAction(nameof(MeuPedido), new { id = pedido.Idatendimento }, ExibirAtendimento(resposta));
        }
        catch (DbUpdateException ex)
        {
            await transacao.RollbackAsync();
            _logger.LogError(ex, "Erro ao confirmar a compra.");
            return Conflict(new { mensagem = "Não foi possível confirmar o pedido. Confira a disponibilidade dos itens." });
        }
    }

    [Authorize(Roles = "Funcionario")]
    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> AtualizarStatus(int id, PedidoStatusDTO dados)
    {
        await using var transacao = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            var pedido = await BuscarAtendimentoParaAlterar(id);
            if (pedido == null) { return NotFound(new { mensagem = "Pedido não localizado." }); }
            var atual = pedido.StatusAtendimento ?? "A";
            var proximo = dados.StatusAtendimento;
            if (atual == proximo) { return NoContent(); }
            var permitido = (atual == "A" && (proximo == "P" || proximo == "C"))
                || (atual == "P" && (proximo == "F" || proximo == "C"));
            if (!permitido)
            {
                return Conflict(new { mensagem = "Transição inválida. Use Aberto → Em processamento → Concluído, ou cancele antes de concluir." });
            }
            if (proximo == "C")
            {
                var ids = pedido.Atendimentoitems.Select(i => i.Iditem).ToList();
                var produtos = await _context.Produtos.Where(p => ids.Contains(p.Iditem)).Select(p => p.Iditem).ToListAsync();
                var estoques = await BloquearEstoques(produtos);
                if (estoques.Count != produtos.Count) { return Conflict(new { mensagem = "Um produto não possui registro de estoque." }); }
                foreach (var estoque in estoques)
                {
                    var quantidade = pedido.Atendimentoitems.First(i => i.Iditem == estoque.Iditem).QuantidadeItem;
                    long saldo = (long)estoque.QuantidadeEstoque + quantidade;
                    if (saldo > int.MaxValue) { return Conflict(new { mensagem = "O estorno ultrapassa o limite do estoque." }); }
                    estoque.QuantidadeEstoque = (int)saldo;
                }
            }
            pedido.StatusAtendimento = proximo;
            await _context.SaveChangesAsync();
            await transacao.CommitAsync();
            return NoContent();
        }
        catch (DbUpdateException ex)
        {
            await transacao.RollbackAsync();
            _logger.LogError(ex, "Erro ao atualizar a situação do pedido.");
            return Conflict(new { mensagem = "Não foi possível alterar a situação do pedido." });
        }
    }

    private string? NormalizarStatus(string status)
    {
        if (status.Equals("Aberto", StringComparison.OrdinalIgnoreCase) || status == "A") { return "A"; }
        if (status.Equals("Em processamento", StringComparison.OrdinalIgnoreCase) || status == "P") { return "P"; }
        if (status.Equals("Concluído", StringComparison.OrdinalIgnoreCase) || status == "F") { return "F"; }
        if (status.Equals("Cancelado", StringComparison.OrdinalIgnoreCase) || status == "C") { return "C"; }
        return null;
    }

    // Os métodos privados abaixo pertencem somente a este controller.
    // Evitam repetir as mesmas consultas e regras nas oito rotas acima.
    private IQueryable<Atendimento> ConsultarAtendimentos()
    {
        return _context.Atendimentos
            .AsNoTracking()
            .AsSplitQuery()
            .Include(a => a.IdpessoaNavigation).ThenInclude(p => p.Enderecos)
            .Include(a => a.Atendimentoitems).ThenInclude(i => i.IditemNavigation)
            .Include(a => a.Atendimentoitems).ThenInclude(i => i.IdfuncionarioNavigation)
                .ThenInclude(f => f.IdpessoaNavigation);
    }

    private async Task<Atendimento?> BuscarAtendimentoParaAlterar(int id)
    {
        var registros = await _context.Atendimentos
            .FromSqlInterpolated($"SELECT * FROM atendimento WHERE idatendimento = {id} FOR UPDATE")
            .ToListAsync();
        var atendimento = registros.FirstOrDefault();

        if (atendimento != null)
        {
            await _context.Entry(atendimento).Collection(a => a.Atendimentoitems).LoadAsync();
        }

        return atendimento;
    }

    private List<AtendimentoItemDTO> CopiarItens(Atendimento atendimento)
    {
        var itens = new List<AtendimentoItemDTO>();
        foreach (var item in atendimento.Atendimentoitems)
        {
            itens.Add(new AtendimentoItemDTO
            {
                IdItem = item.Iditem,
                IdFuncionario = item.Idfuncionario,
                QuantidadeItem = item.QuantidadeItem,
                ValorUnitarioEntrada = item.ValorunitarioEntrada ?? 0
            });
        }

        return itens;
    }

    // Retorna uma resposta de erro se algo falhar; null significa que deu certo.
    // É chamado dentro da transação iniciada pela rota.
    private async Task<IActionResult?> AtualizarItens(Atendimento atendimento, List<AtendimentoItemDTO> dados, bool permitirVazio = false)
    {
        if (dados == null || (!permitirVazio && dados.Count == 0) || dados.Any(i => i == null))
        {
            return BadRequest(new { mensagem = "Informe pelo menos um item válido." });
        }

        var idsRecebidos = new List<int>();
        foreach (var item in dados)
        {
            if (item.IdItem <= 0 || item.IdFuncionario <= 0 || item.QuantidadeItem <= 0
                || item.ValorUnitarioEntrada == null || item.ValorUnitarioEntrada < 0 || item.ValorUnitarioEntrada > 99999999.99m)
            {
                return BadRequest(new { mensagem = "Confira os IDs, as quantidades positivas e os preços dos itens." });
            }

            if (idsRecebidos.Contains(item.IdItem))
            {
                return BadRequest(new { mensagem = "Cada item só pode aparecer uma vez. Some as quantidades." });
            }

            idsRecebidos.Add(item.IdItem);
            if (!await _context.Funcionarios.AnyAsync(f => f.Idpessoa == item.IdFuncionario))
            {
                return NotFound(new { mensagem = "Funcionário não localizado." });
            }
        }

        var itensAnteriores = atendimento.Atendimentoitems.ToList();
        var idsTodos = new List<int>(idsRecebidos);
        foreach (var item in itensAnteriores)
        {
            if (!idsTodos.Contains(item.Iditem))
            {
                idsTodos.Add(item.Iditem);
            }
        }

        var itensCadastrados = await _context.Items
            .Include(i => i.Produto)
            .AsNoTracking()
            .Where(i => idsTodos.Contains(i.Iditem))
            .ToListAsync();

        if (itensCadastrados.Count != idsTodos.Count)
        {
            return NotFound(new { mensagem = "Um dos produtos ou serviços não existe." });
        }

        var idsProdutos = new List<int>();
        foreach (var item in itensCadastrados)
        {
            if (item.TipoItem == "P" && item.Produto == null && idsRecebidos.Contains(item.Iditem))
            {
                return Conflict(new { mensagem = "Um dos itens do tipo P ainda não possui cadastro de produto." });
            }

            if (item.Produto != null)
            {
                idsProdutos.Add(item.Iditem);
            }
        }

        var estoques = await BloquearEstoques(idsProdutos);
        if (estoques.Count != idsProdutos.Count)
        {
            return Conflict(new { mensagem = "Produto sem estoque. Confira a trigger de cadastro." });
        }

        // Confere o saldo final de cada produto antes de gravar qualquer alteração.
        foreach (var estoque in estoques)
        {
            var anterior = itensAnteriores.FirstOrDefault(i => i.Iditem == estoque.Iditem);
            var novo = dados.FirstOrDefault(i => i.IdItem == estoque.Iditem);
            int quantidadeAnterior = 0;
            int quantidadeNova = 0;

            if (anterior != null)
            {
                quantidadeAnterior = anterior.QuantidadeItem;
            }

            if (novo != null)
            {
                quantidadeNova = novo.QuantidadeItem;
            }

            long saldoFinal = (long)estoque.QuantidadeEstoque + quantidadeAnterior - quantidadeNova;
            if (saldoFinal < 0 || saldoFinal > int.MaxValue)
            {
                return Conflict(new { mensagem = $"Estoque insuficiente ou fora do limite para o produto {estoque.Iditem}." });
            }
        }

        // Vínculos antigos: o PUT e o DELETE precisam ajustar o estoque aqui.
        foreach (var anterior in itensAnteriores)
        {
            var novo = dados.FirstOrDefault(i => i.IdItem == anterior.Iditem);
            var estoque = estoques.FirstOrDefault(e => e.Iditem == anterior.Iditem);

            if (novo == null)
            {
                if (estoque != null)
                {
                    estoque.QuantidadeEstoque = (int)((long)estoque.QuantidadeEstoque + anterior.QuantidadeItem);
                }

                _context.Atendimentoitems.Remove(anterior);
            }
            else
            {
                if (estoque != null)
                {
                    estoque.QuantidadeEstoque = (int)((long)estoque.QuantidadeEstoque + anterior.QuantidadeItem - novo.QuantidadeItem);
                }

                anterior.Idfuncionario = novo.IdFuncionario;
                anterior.QuantidadeItem = novo.QuantidadeItem;
                anterior.ValorunitarioEntrada = novo.ValorUnitarioEntrada;
            }
        }

        await _context.SaveChangesAsync();

        // Só os vínculos novos disparam a baixa de estoque feita pela trigger.
        foreach (var novo in dados)
        {
            if (!itensAnteriores.Any(i => i.Iditem == novo.IdItem))
            {
                _context.Atendimentoitems.Add(new Atendimentoitem
                {
                    Idatendimento = atendimento.Idatendimento,
                    Iditem = novo.IdItem,
                    Idfuncionario = novo.IdFuncionario,
                    QuantidadeItem = novo.QuantidadeItem,
                    ValorunitarioEntrada = novo.ValorUnitarioEntrada
                });
            }
        }

        await _context.SaveChangesAsync();
        return null;
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

    private object ExibirAtendimento(Atendimento atendimento)
    {
        var itens = new List<object>();
        decimal valorTotal = 0;

        foreach (var item in atendimento.Atendimentoitems.OrderBy(i => i.Iditem))
        {
            var subtotal = item.QuantidadeItem * (item.ValorunitarioEntrada ?? 0);
            itens.Add(new
            {
                IdItem = item.Iditem,
                Item = item.IditemNavigation.Item1,
                TipoItem = item.IditemNavigation.TipoItem,
                IdFuncionario = item.Idfuncionario,
                Funcionario = item.IdfuncionarioNavigation.IdpessoaNavigation.Nome,
                CargoFuncionario = item.IdfuncionarioNavigation.Cargo,
                item.QuantidadeItem,
                ValorUnitarioEntrada = item.ValorunitarioEntrada,
                Subtotal = subtotal
            });

            valorTotal += subtotal;
        }

        return new
        {
            IdAtendimento = atendimento.Idatendimento,
            IdPessoa = atendimento.Idpessoa,
            Cliente = atendimento.IdpessoaNavigation.Nome,
            Pessoa = ExibirPessoa(atendimento.IdpessoaNavigation),
            atendimento.DataAtendimento,
            atendimento.StatusAtendimento,
            Itens = itens,
            ValorTotal = valorTotal
        };
    }

    // Monta o JSON de saída sem devolver os relacionamentos circulares do EF.
    private object ExibirPessoa(Pessoa pessoa)
    {
        return new
        {
            pessoa.Idpessoa,
            pessoa.Nome,
            pessoa.Cpf,
            pessoa.DataNascimento,
            pessoa.Sexo,
            pessoa.Email,
            pessoa.Telefone,
            pessoa.AtivoPessoa,
            Enderecos = pessoa.Enderecos.OrderBy(e => e.Idendereco).Select(e => new
            {
                e.Idendereco,
                e.Cep,
                e.Numero,
                e.Complemento
            }).ToList()
        };
    }
}
