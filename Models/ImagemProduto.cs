namespace AplicacaoBackEnd.Models;

public class ImagemProduto
{
    public int Iditem { get; set; }
    public string NomeArquivo { get; set; } = string.Empty;
    public string TipoConteudo { get; set; } = string.Empty;
    [System.Text.Json.Serialization.JsonIgnore]
    public byte[] Dados { get; set; } = Array.Empty<byte>();
    public string Versao { get; set; } = string.Empty;
    public DateTime AtualizadoEm { get; set; }
}
