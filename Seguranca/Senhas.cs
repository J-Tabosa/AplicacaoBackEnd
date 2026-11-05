using System.Security.Cryptography;
using System.Text;

namespace AplicacaoBackEnd.Seguranca;

public static class Senhas
{
    // Esta marca identifica a versão da senha na sessão. Não é o hash usado no login.
    // Ao mudar a senha, a marca muda e os cookies antigos deixam de ser válidos.
    public static string Marca(string hash) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(hash)));

    public static string LerNoTerminal()
    {
        if (Console.IsInputRedirected) return Console.ReadLine() ?? string.Empty;
        var senha = new StringBuilder();
        while (true)
        {
            var tecla = Console.ReadKey(intercept: true);
            if (tecla.Key == ConsoleKey.Enter) { Console.WriteLine(); return senha.ToString(); }
            if (tecla.Key == ConsoleKey.Backspace)
            {
                if (senha.Length > 0) senha.Length--;
            }
            else if (!char.IsControl(tecla.KeyChar)) senha.Append(tecla.KeyChar);
        }
    }
}
