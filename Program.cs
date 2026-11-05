using AplicacaoBackEnd.Seguranca;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using AplicacaoBackEnd.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.Configure<PasswordHasherOptions>(options => options.IterationCount = 210000);
builder.Services.AddScoped<IPasswordHasher<Pessoa>, PasswordHasher<Pessoa>>();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, token) =>
        await context.HttpContext.Response.WriteAsJsonAsync(new { mensagem = "Muitas tentativas de login. Aguarde um minuto e tente novamente." }, token);
    options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "local",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = builder.Configuration.GetValue<int?>("Login:LimitePorMinuto") ?? 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<DbAtendimentoContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("connection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("connection"))
    )
);

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "Estetica.Sessao.v2";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.None : CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = 401;
            return context.Response.WriteAsJsonAsync(new { mensagem = "Identifique-se para continuar." });
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = 403;
            return context.Response.WriteAsJsonAsync(new { mensagem = "Acesso restrito aos funcionários." });
        };
        options.Events.OnValidatePrincipal = async context =>
        {
            var db = context.HttpContext.RequestServices.GetRequiredService<DbAtendimentoContext>();
            int.TryParse(context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id);
            var pessoa = await db.Pessoas.AsNoTracking()
                .Where(p => p.Idpessoa == id && p.AtivoPessoa == "S")
                .Select(p => new { p.SenhaHash }).FirstOrDefaultAsync();
            var senhaValida = !string.IsNullOrEmpty(pessoa?.SenhaHash)
                && context.Principal?.FindFirstValue("senha_versao") == Senhas.Marca(pessoa.SenhaHash);
            var funcionarioValido = context.Principal?.IsInRole("Funcionario") != true
                || await db.Funcionarios.AnyAsync(f => f.Idpessoa == id);
            if (!senhaValida || !funcionarioValido)
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// Comando local para o primeiro acesso de cadastros existentes. Não abre um endpoint público.
if (args.Contains("--definir-senha"))
{
    var indice = Array.IndexOf(args, "--definir-senha");
    var cpfTexto = indice + 1 < args.Length ? args[indice + 1] : string.Empty;
    if (string.IsNullOrWhiteSpace(cpfTexto)) { Console.Write("CPF do cadastro: "); cpfTexto = Console.ReadLine() ?? string.Empty; }
    if (!long.TryParse(cpfTexto, out var cpf) || cpf <= 0 || cpf > 99999999999)
    {
        Console.Error.WriteLine("Informe um CPF válido, somente com números.");
        Environment.ExitCode = 1;
        return;
    }
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<DbAtendimentoContext>();
    var pessoa = await db.Pessoas.FirstOrDefaultAsync(p => p.Cpf == cpf);
    if (pessoa == null) { Console.Error.WriteLine("CPF não localizado."); Environment.ExitCode = 1; return; }
    Console.Write("Nova senha (8 a 128 caracteres): ");
    var senha = Senhas.LerNoTerminal();
    Console.Write("Confirme a senha: ");
    var confirmacao = Senhas.LerNoTerminal();
    if (senha.Length < 8 || senha.Length > 128 || senha != confirmacao)
    {
        Console.Error.WriteLine("As senhas devem coincidir e ter de 8 a 128 caracteres.");
        Environment.ExitCode = 1;
        return;
    }
    pessoa.SenhaHash = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Pessoa>>().HashPassword(pessoa, senha);
    await db.SaveChangesAsync();
    Console.WriteLine("Senha definida. Agora faça login pelo site.");
    return;
}

app.UseCors(o => o.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
