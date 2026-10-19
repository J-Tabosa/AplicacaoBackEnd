// DTO: recebe os campos enviados no JSON.
// Models: contém as classes que representam as tabelas do banco.
using AplicacaoBackEnd.DTO;
using AplicacaoBackEnd.Models;

// MVC fornece ControllerBase, as rotas e as respostas HTTP.
// EntityFrameworkCore fornece os métodos de consulta e gravação no banco.
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

// Agrupa esta classe com os outros controllers do projeto.
namespace AplicacaoBackEnd.Controllers;

// [controller] é substituído pelo nome da classe sem "Controller".
// Assim, o endereço base deste controller é /api/Categoriums.
[Route("api/[controller]")]

// Indica que esta classe é um controller de API.
// Também valida o DTO antes de executar o método: se for inválido, retorna 400.
[ApiController]

// ControllerBase fornece Ok, NotFound, BadRequest, Conflict, entre outros.
public class CategoriumsController : ControllerBase
{
    // _context permite acessar as tabelas do banco usando o Entity Framework.
    // readonly impede trocar essa referência depois do construtor.
    private readonly DbAtendimentoContext _context;

    // _logger registra os detalhes dos erros no log da aplicação.
    private readonly ILogger<CategoriumsController> _logger;

    // O ASP.NET entrega essas dependências quando cria o controller.
    // O contexto foi configurado em Program.cs; o logger é fornecido pelo ASP.NET.
    public CategoriumsController(DbAtendimentoContext context, ILogger<CategoriumsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET /api/Categoriums: lista todas as categorias.
    [HttpGet]

    // async/await permitem esperar o banco sem bloquear a execução.
    // Task representa a operação assíncrona; IActionResult permite retornar
    // diferentes respostas HTTP, como 200, 404 e 409.
    public async Task<IActionResult> GetCategoria()
    {
        var categorias = await _context.Categoria
            // Consulta para leitura: não acompanha as categorias para atualização.
            .AsNoTracking()

            // Ordena os resultados pelo ID.
            // c => c.Idcategoria significa: para cada categoria c, use seu ID.
            .OrderBy(c => c.Idcategoria)

            // Escolhe os campos que serão devolvidos.
            // new { ... } cria um objeto sem precisar de uma classe DTO de resposta.
            // Não incluímos os relacionamentos do Model para evitar referências circulares.
            .Select(c => new
            {
                c.Idcategoria,
                c.Categoria
            })

            // Executa a consulta no banco e reúne os resultados em uma lista.
            .ToListAsync();

        // Retorna HTTP 200 com a lista no corpo da resposta, em JSON.
        // Se não houver categorias, a lista será vazia.
        return Ok(categorias);
    }

    // GET /api/Categoriums/5: procura uma categoria pelo ID.
    // {id:int} aceita um número inteiro na URL e o entrega ao parâmetro id.
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetCategorium(int id)
    {
        // FindAsync procura pela chave primária da tabela: Idcategoria.
        var categoria = await _context.Categoria.FindAsync(id);

        // null significa que o registro não foi encontrado.
        if (categoria == null)
        {
            // HTTP 404 com uma mensagem explicando o problema.
            return NotFound(new { mensagem = "Categoria não localizada." });
        }

        // HTTP 200 com somente o ID e o nome da categoria.
        return Ok(new
        {
            categoria.Idcategoria,
            categoria.Categoria
        });
    }

    // POST /api/Categoriums: cadastra uma categoria.
    [HttpPost]
    public async Task<IActionResult> PostCategorium(CategoriaDTO dados)
    {
        // dados recebe o JSON da requisição e é validado pelos atributos do DTO.
        // O DTO representa a entrada; Categorium representa a tabela.
        var categoria = new Categorium
        {
            Categoria = dados.Categoria
        };

        // try executa a gravação; catch trata uma falha do banco.
        try
        {
            // Add marca a categoria para inclusão. Ainda não grava no banco.
            _context.Categoria.Add(categoria);

            // Executa o INSERT. O banco gera o ID e o EF o preenche no objeto.
            await _context.SaveChangesAsync();

            // HTTP 201: informa que o registro foi criado.
            // nameof obtém o nome do método que consulta esse registro.
            // new { id = ... } fornece o ID para montar a URL no cabeçalho Location.
            // O último argumento é o objeto devolvido no corpo da resposta.
            return CreatedAtAction(
                nameof(GetCategorium),
                new { id = categoria.Idcategoria },
                new
                {
                    categoria.Idcategoria,
                    categoria.Categoria
                });
        }
        catch (DbUpdateException ex)
        {
            // Registra o erro técnico no log e devolve uma mensagem ao cliente.
            // HTTP 409 indica um conflito ao gravar os dados.
            _logger.LogError(ex, "Erro ao cadastrar a categoria.");
            return Conflict(new { mensagem = "Não foi possível cadastrar a categoria. Confira os dados." });
        }
    }

    // PUT /api/Categoriums/5: atualiza a categoria indicada pela URL.
    [HttpPut("{id:int}")]
    public async Task<IActionResult> PutCategorium(int id, CategoriaDTO dados)
    {
        var categoria = await _context.Categoria.FindAsync(id);
        if (categoria == null)
        {
            return NotFound(new { mensagem = "Categoria não localizada." });
        }

        try
        {
            // Altera o nome no objeto que foi carregado do banco.
            // O EF acompanha esse objeto e identifica a mudança.
            categoria.Categoria = dados.Categoria;

            // Executa o UPDATE.
            await _context.SaveChangesAsync();

            // HTTP 204: operação concluída, sem corpo na resposta.
            return NoContent();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Erro ao atualizar a categoria.");
            return Conflict(new { mensagem = "Não foi possível atualizar a categoria. Consulte novamente." });
        }
    }

    // DELETE /api/Categoriums/5: exclui a categoria indicada pela URL.
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteCategorium(int id)
    {
        var categoria = await _context.Categoria.FindAsync(id);
        if (categoria == null)
        {
            return NotFound(new { mensagem = "Categoria não localizada." });
        }

        // AnyAsync verifica se existe pelo menos um item ligado a esta categoria.
        // Não podemos excluir uma categoria que ainda é usada por produto ou serviço.
        if (await _context.Items.AnyAsync(i => i.Idcategoria == id))
        {
            return Conflict(new { mensagem = "A categoria possui produtos ou serviços vinculados." });
        }

        try
        {
            // Remove marca o registro para exclusão; SaveChangesAsync executa o DELETE.
            _context.Categoria.Remove(categoria);
            await _context.SaveChangesAsync();
            return NoContent();
        }
        catch (DbUpdateException ex)
        {
            // Mesmo após a conferência acima, o banco pode recusar a exclusão,
            // por exemplo se outro pedido cadastrar um vínculo nesse intervalo.
            _logger.LogError(ex, "Erro ao excluir a categoria.");
            return Conflict(new { mensagem = "Não foi possível excluir a categoria. Confira seus vínculos." });
        }
    }
}
