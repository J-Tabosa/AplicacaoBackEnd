using AplicacaoBackEnd.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace AplicacaoBackEnd.Services;

public class RegraNegocioException : Exception
{
    public int Codigo { get; }
    public RegraNegocioException(int codigo, string mensagem) : base(mensagem) => Codigo = codigo;
}

public static class Validacao
{
    public static DateOnly Data(DateOnly? data, string campo)
    {
        if (!data.HasValue || data.Value.Year < 1000)
            throw new RegraNegocioException(400, $"Informe uma data válida para {campo}.");
        return data.Value;
    }
}

// Centraliza respostas de erro sem devolver detalhes internos do banco ao cliente.
public class ApiExceptionFilter : IExceptionFilter
{
    private readonly ILogger<ApiExceptionFilter> _logger;
    public ApiExceptionFilter(ILogger<ApiExceptionFilter> logger) => _logger = logger;

    public void OnException(ExceptionContext context)
    {
        var ex = context.Exception;
        int codigo;
        string mensagem;
        if (ex is RegraNegocioException regra)
        {
            codigo = regra.Codigo;
            mensagem = regra.Message;
        }
        else if (ex is DbUpdateConcurrencyException)
        {
            codigo = 409;
            mensagem = "Os dados foram alterados ou excluídos. Consulte novamente.";
        }
        else
        {
            var sql = ex.GetBaseException() as MySqlException;
            codigo = sql?.Number switch
            {
                1062 or 1451 or 1452 or 1644 or 1205 or 1213 => 409,
                1048 or 1264 or 1406 => 400,
                _ => 500
            };
            mensagem = sql?.Number switch
            {
                1062 => "Já existe um registro com os identificadores informados.",
                1451 or 1452 => "A operação conflita com registros relacionados no banco.",
                1644 => "A operação foi recusada pelas regras do banco. Verifique o estoque e os dados informados.",
                1205 or 1213 => "Outra operação está usando esses registros. Tente novamente.",
                1048 or 1264 or 1406 => "Há campos ausentes ou valores fora dos limites do banco.",
                _ => "Não foi possível concluir a operação. Consulte o log da aplicação."
            };
            _logger.LogError(ex, "Falha na operação da API.");
        }
        context.Result = new ObjectResult(new { mensagem }) { StatusCode = codigo };
        context.ExceptionHandled = true;
    }
}
