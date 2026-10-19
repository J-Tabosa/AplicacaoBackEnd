using AplicacaoBackEnd.DTO;
using AplicacaoBackEnd.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace AplicacaoBackEnd.Controllers;

[Route("api/[controller]")]
[ApiController]
public class FuncionarioController : ControllerBase
{
    private readonly DbAtendimentoContext _context;
    private readonly ILogger<FuncionarioController> _logger;

    public FuncionarioController(DbAtendimentoContext context, ILogger<FuncionarioController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/Funcionario/all
    [HttpGet("all")]
    public async Task<IActionResult> GetFuncionarios()
    {
        var registros = await _context.Funcionarios
            .Include(f => f.IdpessoaNavigation)
            .ThenInclude(p => p.Enderecos)
            .AsNoTracking()
            .OrderBy(r => r.Idpessoa)
            .ToListAsync();

        var resposta = new List<object>();
        foreach (var registro in registros)
        {
            resposta.Add(ExibirFuncionario(registro));
        }

        return Ok(resposta);
    }

    // GET: api/Funcionario/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetFuncionario(int id)
    {
        var funcionario = await _context.Funcionarios
            .Include(f => f.IdpessoaNavigation)
            .ThenInclude(p => p.Enderecos)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Idpessoa == id);

        if (funcionario == null)
        {
            return NotFound(new { mensagem = "Registro não localizado." });
        }

        return Ok(ExibirFuncionario(funcionario));
    }

    // POST: api/Funcionario
    [HttpPost]
    public async Task<IActionResult> PostFuncionario(FuncionarioCadastroDTO dados)
    {
        if (dados.DataNascimento == null || dados.DataNascimento.Value.Year < 1000)
        {
            return BadRequest(new { mensagem = "Informe uma data de nascimento válida." });
        }

        if (dados.DataAdmissao == null || dados.DataAdmissao.Value.Year < 1000)
        {
            return BadRequest(new { mensagem = "Informe uma data de admissão válida." });
        }

        if (dados.Enderecos == null || dados.Enderecos.Count == 0 || dados.Enderecos.Any(e => e == null))
        {
            return BadRequest(new { mensagem = "Informe pelo menos um endereço válido." });
        }

        if (dados.Enderecos.Any(e => e.Idendereco != 0))
        {
            return BadRequest(new { mensagem = "Endereços novos devem ser enviados sem ID ou com ID zero." });
        }

        if (await _context.Pessoas.AnyAsync(p => p.Cpf == dados.Cpf))
        {
            return Conflict(new { mensagem = "Já existe uma pessoa com este CPF." });
        }

        // Ou grava todas as tabelas, ou desfaz tudo ao sair sem CommitAsync.
        await using var transacao = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            var pessoa = new Pessoa();
            pessoa.Nome = dados.Nome;
            pessoa.Cpf = dados.Cpf;
            pessoa.DataNascimento = dados.DataNascimento.GetValueOrDefault();
            pessoa.Sexo = dados.Sexo;
            pessoa.Email = dados.Email;
            pessoa.Telefone = dados.Telefone;
            pessoa.AtivoPessoa = dados.AtivoPessoa;

            foreach (var endereco in dados.Enderecos)
            {
                pessoa.Enderecos.Add(new Endereco
                {
                    Cep = endereco.Cep,
                    Numero = endereco.Numero,
                    Complemento = endereco.Complemento
                });
            }

            _context.Pessoas.Add(pessoa);
            await _context.SaveChangesAsync();

            // Usa o ID da pessoa que acabou de ser criada.
            var funcionario = new Funcionario
            {
                Idpessoa = pessoa.Idpessoa,
                IdpessoaNavigation = pessoa,
                Cargo = dados.Cargo,
                DataAdmissao = dados.DataAdmissao.GetValueOrDefault()
            };

            _context.Funcionarios.Add(funcionario);
            await _context.SaveChangesAsync();

            await transacao.CommitAsync();
            return CreatedAtAction(nameof(GetFuncionario), new { id = pessoa.Idpessoa }, ExibirFuncionario(funcionario));
        }
        catch (DbUpdateException ex)
        {
            await transacao.RollbackAsync();
            _logger.LogError(ex, "Erro ao gravar os dados no banco.");
            return Conflict(new { mensagem = "Não foi possível gravar. Confira os vínculos e se os dados já foram cadastrados." });
        }
    }

    // PUT: api/Funcionario/5
    // A lista de endereços recebida será a lista final desse cadastro.
    [HttpPut("{id:int}")]
    public async Task<IActionResult> PutFuncionario(int id, FuncionarioCadastroDTO dados)
    {
        if (dados.DataNascimento == null || dados.DataNascimento.Value.Year < 1000)
        {
            return BadRequest(new { mensagem = "Informe uma data de nascimento válida." });
        }

        if (dados.DataAdmissao == null || dados.DataAdmissao.Value.Year < 1000)
        {
            return BadRequest(new { mensagem = "Informe uma data de admissão válida." });
        }

        if (dados.Enderecos == null || dados.Enderecos.Count == 0 || dados.Enderecos.Any(e => e == null))
        {
            return BadRequest(new { mensagem = "Informe pelo menos um endereço válido." });
        }

        await using var transacao = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            var funcionarios = await _context.Funcionarios
                .FromSqlInterpolated($"SELECT * FROM funcionario WHERE idpessoa = {id} FOR UPDATE")
                .ToListAsync();
            var funcionario = funcionarios.FirstOrDefault();

            if (funcionario == null)
            {
                return NotFound(new { mensagem = "Funcionário não localizado." });
            }

            // FOR UPDATE impede duas alterações simultâneas nesta pessoa.
            var pessoas = await _context.Pessoas
                .FromSqlInterpolated($"SELECT * FROM pessoa WHERE idpessoa = {id} FOR UPDATE")
                .ToListAsync();
            var pessoa = pessoas.FirstOrDefault();

            if (pessoa == null)
            {
                return NotFound(new { mensagem = "Pessoa não localizada." });
            }

            await _context.Entry(pessoa).Collection(p => p.Enderecos).LoadAsync();

            if (await _context.Pessoas.AnyAsync(p => p.Cpf == dados.Cpf && p.Idpessoa != id))
            {
                return Conflict(new { mensagem = "Já existe outra pessoa com este CPF." });
            }

            var idsRecebidos = new List<int>();
            foreach (var endereco in dados.Enderecos)
            {
                if (endereco.Idendereco == 0)
                {
                    continue; // ID zero significa endereço novo.
                }

                if (idsRecebidos.Contains(endereco.Idendereco))
                {
                    return BadRequest(new { mensagem = "Não repita o ID de um endereço." });
                }

                if (!pessoa.Enderecos.Any(e => e.Idendereco == endereco.Idendereco))
                {
                    return BadRequest(new { mensagem = "O endereço não existe ou pertence a outra pessoa." });
                }

                idsRecebidos.Add(endereco.Idendereco);
            }

            pessoa.Nome = dados.Nome;
            pessoa.Cpf = dados.Cpf;
            pessoa.DataNascimento = dados.DataNascimento.GetValueOrDefault();
            pessoa.Sexo = dados.Sexo;
            pessoa.Email = dados.Email;
            pessoa.Telefone = dados.Telefone;
            pessoa.AtivoPessoa = dados.AtivoPessoa;

            // Remove somente os endereços antigos que não vieram no PUT.
            foreach (var endereco in pessoa.Enderecos.ToList())
            {
                if (!idsRecebidos.Contains(endereco.Idendereco))
                {
                    _context.Enderecos.Remove(endereco);
                }
            }

            foreach (var dadosEndereco in dados.Enderecos)
            {
                Endereco endereco;
                if (dadosEndereco.Idendereco == 0)
                {
                    endereco = new Endereco
                    {
                        Idpessoa = pessoa.Idpessoa,
                        IdpessoaNavigation = pessoa
                    };
                    _context.Enderecos.Add(endereco);
                }
                else
                {
                    endereco = pessoa.Enderecos.First(e => e.Idendereco == dadosEndereco.Idendereco);
                }

                endereco.Cep = dadosEndereco.Cep;
                endereco.Numero = dadosEndereco.Numero;
                endereco.Complemento = dadosEndereco.Complemento;
            }

            funcionario.Cargo = dados.Cargo;
            funcionario.DataAdmissao = dados.DataAdmissao.GetValueOrDefault();

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

    // DELETE: api/Funcionario/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteFuncionario(int id)
    {
        await using var transacao = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            var registros = await _context.Funcionarios
                .FromSqlInterpolated($"SELECT * FROM funcionario WHERE idpessoa = {id} FOR UPDATE")
                .ToListAsync();
            var funcionario = registros.FirstOrDefault();

            if (funcionario == null)
            {
                return NotFound(new { mensagem = "Registro não localizado." });
            }

            if (await _context.Atendimentoitems.AnyAsync(a => a.Idfuncionario == id))
            {
                return Conflict(new { mensagem = "O funcionário possui atendimentos e não pode ser excluído." });
            }

            // Exclui o vínculo de funcionário. Pessoa e endereços continuam cadastrados.
            _context.Funcionarios.Remove(funcionario);

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

    // Monta o JSON de saída sem devolver os relacionamentos circulares do EF.
    private object ExibirFuncionario(Funcionario funcionario)
    {
        var pessoa = funcionario.IdpessoaNavigation;

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
            Cargo = funcionario.Cargo,
            DataAdmissao = funcionario.DataAdmissao,
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
