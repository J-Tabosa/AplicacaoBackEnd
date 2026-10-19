namespace AplicacaoBackEnd.DTO
{
    // Dados devolvidos no JSON da resposta.
    public class EnderecoRespostaDTO
    {
        public int Idendereco { get; set; }

        public string Cep { get; set; } = string.Empty;

        public string? Numero { get; set; }

        public string? Complemento { get; set; }
    }
}
