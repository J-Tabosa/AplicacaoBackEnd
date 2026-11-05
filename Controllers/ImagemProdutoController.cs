using AplicacaoBackEnd.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Buffers.Binary;

namespace AplicacaoBackEnd.Controllers;

[ApiController]
[Route("api/Items/{id:int}/imagem")]
public class ImagemProdutoController : ControllerBase
{
    private const int LimiteImagem = 2 * 1024 * 1024;
    private readonly DbAtendimentoContext _context;

    public ImagemProdutoController(DbAtendimentoContext context) { _context = context; }

    [HttpGet]
    public async Task<IActionResult> Consultar(int id)
    {
        var produtoVisivel = await _context.Items.AnyAsync(i => i.Iditem == id && i.TipoItem == "P"
            && (i.AtivoProdutoServico == "S" || User.IsInRole("Funcionario")));
        if (!produtoVisivel) return NotFound(new { mensagem = "Imagem não localizada." });
        var imagem = await _context.ImagensProdutos.AsNoTracking().FirstOrDefaultAsync(i => i.Iditem == id);
        if (imagem == null) return NotFound(new { mensagem = "Produto sem imagem cadastrada." });
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers.CacheControl = "no-store";
        return File(imagem.Dados, imagem.TipoConteudo);
    }

    [Authorize(Roles = "Funcionario")]
    [HttpPut]
    [RequestSizeLimit(LimiteImagem + 65536)]
    [RequestFormLimits(MultipartBodyLengthLimit = LimiteImagem + 65536)]
    public async Task<IActionResult> Salvar(int id, IFormFile arquivo)
    {
        if (arquivo == null || arquivo.Length == 0 || arquivo.Length > LimiteImagem)
            return BadRequest(new { mensagem = "Selecione uma imagem PNG, JPEG ou WebP de até 2 MiB." });
        await using var memoria = new MemoryStream();
        await arquivo.CopyToAsync(memoria);
        var dados = memoria.ToArray();
        var tipo = DetectarTipo(dados);
        if (tipo == null || (!string.IsNullOrEmpty(arquivo.ContentType) && arquivo.ContentType != tipo))
            return BadRequest(new { mensagem = "O conteúdo do arquivo deve ser uma imagem PNG, JPEG ou WebP válida." });
        var extensao = Path.GetExtension(arquivo.FileName).ToLowerInvariant();
        var extensaoValida = tipo == "image/png" ? extensao == ".png"
            : tipo == "image/jpeg" ? extensao == ".jpg" || extensao == ".jpeg" : extensao == ".webp";
        if (!extensaoValida) return BadRequest(new { mensagem = "A extensão do arquivo não corresponde ao conteúdo da imagem." });
        var nome = Path.GetFileName(arquivo.FileName.Replace('\\', '/'));
        if (nome.Length > 120) return BadRequest(new { mensagem = "O nome do arquivo deve ter até 120 caracteres." });

        // O bloqueio do produto também serializa duas substituições da mesma imagem.
        await using var transacao = await _context.Database.BeginTransactionAsync();
        var produtos = await _context.Produtos
            .FromSqlInterpolated($"SELECT * FROM produto WHERE iditem = {id} FOR UPDATE").ToListAsync();
        if (produtos.Count == 0) return NotFound(new { mensagem = "Produto não localizado. Serviços não recebem imagem de produto." });
        var imagem = await _context.ImagensProdutos.FirstOrDefaultAsync(i => i.Iditem == id);
        if (imagem == null) { imagem = new ImagemProduto { Iditem = id }; _context.ImagensProdutos.Add(imagem); }
        imagem.NomeArquivo = nome;
        imagem.TipoConteudo = tipo;
        imagem.Dados = dados;
        imagem.Versao = Guid.NewGuid().ToString("N");
        imagem.AtualizadoEm = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        await transacao.CommitAsync();
        return Ok(new { imagemUrl = $"/api/Items/{id}/imagem?v={imagem.Versao}" });
    }

    [Authorize(Roles = "Funcionario")]
    [HttpDelete]
    public async Task<IActionResult> Remover(int id)
    {
        await using var transacao = await _context.Database.BeginTransactionAsync();
        var produtos = await _context.Produtos
            .FromSqlInterpolated($"SELECT * FROM produto WHERE iditem = {id} FOR UPDATE").ToListAsync();
        if (produtos.Count == 0) return NotFound(new { mensagem = "Produto não localizado." });
        var imagem = await _context.ImagensProdutos.FirstOrDefaultAsync(i => i.Iditem == id);
        if (imagem != null) { _context.ImagensProdutos.Remove(imagem); await _context.SaveChangesAsync(); }
        await transacao.CommitAsync();
        return NoContent();
    }

    private static string? DetectarTipo(byte[] dados)
    {
        // Verifica o conteúdo, além da extensão e do MIME enviados pelo navegador.
        byte[] png = { 137, 80, 78, 71, 13, 10, 26, 10 };
        if (dados.Length >= 33 && dados.AsSpan(0, 8).SequenceEqual(png)
            && dados.AsSpan(12, 4).SequenceEqual("IHDR"u8)
            && BinaryPrimitives.ReadUInt32BigEndian(dados.AsSpan(16, 4)) > 0
            && BinaryPrimitives.ReadUInt32BigEndian(dados.AsSpan(20, 4)) > 0) return "image/png";
        if (dados.Length >= 4 && dados[0] == 255 && dados[1] == 216 && dados[2] == 255
            && dados[^2] == 255 && dados[^1] == 217) return "image/jpeg";
        if (dados.Length >= 20 && dados.AsSpan(0, 4).SequenceEqual("RIFF"u8)
            && dados.AsSpan(8, 4).SequenceEqual("WEBP"u8)
            && BinaryPrimitives.ReadUInt32LittleEndian(dados.AsSpan(4, 4)) == dados.Length - 8
            && (dados.AsSpan(12, 4).SequenceEqual("VP8 "u8)
                || dados.AsSpan(12, 4).SequenceEqual("VP8L"u8)
                || dados.AsSpan(12, 4).SequenceEqual("VP8X"u8))) return "image/webp";
        return null;
    }
}
