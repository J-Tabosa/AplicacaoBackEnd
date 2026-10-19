using AplicacaoBackEnd.DTO;
using AplicacaoBackEnd.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;

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

    [HttpGet("all")]
    public async Task<IActionResult> GetAtendimentos()
    {
        var atendimentos = await ConsultarAtendimentos()
            .OrderBy(a => a.Idatendimento)
            .ToListAsync();

        var resposta = new List<AtendimentoRespostaDTO>();
        foreach (var atendimento in atendimentos)
        {
            resposta.Add(ExibirAtendimento(atendimento));
        }

        return Ok(resposta);
    }

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

    [HttpPost]
    public async Task<IActionResult> PostAtendimento(AtendimentoDTO dados)
    {
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

            if (!await _context.Pessoas.AnyAsync(p => p.Idpessoa == dados.IdPessoa))
            {
                return NotFound(new { mensagem = "Pessoa atendida não localizada." });
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

            // Lista vazia remove os vínculos e devolve todos os produtos ao estoque.
            var erro = await AtualizarItens(atendimento, new List<AtendimentoItemDTO>(), true);
            if (erro != null)
            {
                return erro;
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

    // Os métodos privados abaixo pertencem somente a este controller.
    // Evitam repetir as mesmas consultas e regras nas oito rotas acima.
    private IQueryable<Atendimento> ConsultarAtendimentos()
    {
        return _context.Atendimentos
            .AsNoTracking()
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

    private AtendimentoRespostaDTO ExibirAtendimento(Atendimento atendimento)
    {
        var resposta = new AtendimentoRespostaDTO
        {
            IdAtendimento = atendimento.Idatendimento,
            IdPessoa = atendimento.Idpessoa,
            Cliente = atendimento.IdpessoaNavigation.Nome,
            Pessoa = ExibirPessoa(atendimento.IdpessoaNavigation),
            DataAtendimento = atendimento.DataAtendimento,
            StatusAtendimento = atendimento.StatusAtendimento
        };

        foreach (var item in atendimento.Atendimentoitems.OrderBy(i => i.Iditem))
        {
            var subtotal = item.QuantidadeItem * (item.ValorunitarioEntrada ?? 0);
            resposta.Itens.Add(new AtendimentoItemRespostaDTO
            {
                IdItem = item.Iditem,
                Item = item.IditemNavigation.Item1,
                TipoItem = item.IditemNavigation.TipoItem,
                IdFuncionario = item.Idfuncionario,
                Funcionario = item.IdfuncionarioNavigation.IdpessoaNavigation.Nome,
                QuantidadeItem = item.QuantidadeItem,
                ValorUnitarioEntrada = item.ValorunitarioEntrada,
                Subtotal = subtotal
            });

            resposta.ValorTotal += subtotal;
        }

        return resposta;
    }

    // Monta o JSON de saída sem devolver os relacionamentos circulares do EF.
    private PessoaRespostaDTO ExibirPessoa(Pessoa pessoa)
    {
        var resposta = new PessoaRespostaDTO
        {
            Idpessoa = pessoa.Idpessoa,
            Nome = pessoa.Nome,
            Cpf = pessoa.Cpf,
            DataNascimento = pessoa.DataNascimento,
            Sexo = pessoa.Sexo,
            Email = pessoa.Email,
            Telefone = pessoa.Telefone,
            AtivoPessoa = pessoa.AtivoPessoa
        };

        foreach (var endereco in pessoa.Enderecos.OrderBy(e => e.Idendereco))
        {
            resposta.Enderecos.Add(new EnderecoRespostaDTO
            {
                Idendereco = endereco.Idendereco,
                Cep = endereco.Cep,
                Numero = endereco.Numero,
                Complemento = endereco.Complemento
            });
        }

        return resposta;
    }
}
