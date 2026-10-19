namespace AplicacaoBackEnd.DTO
{
    // Dados devolvidos no JSON da resposta.
    public class AtendimentoRespostaDTO
    {
        public int IdAtendimento { get; set; }

        public int IdPessoa { get; set; }

        public string Cliente { get; set; } = string.Empty;

        public PessoaRespostaDTO Pessoa { get; set; } = new PessoaRespostaDTO();

        public DateOnly DataAtendimento { get; set; }

        public string? StatusAtendimento { get; set; }

        public List<AtendimentoItemRespostaDTO> Itens { get; set; } = new List<AtendimentoItemRespostaDTO>();

        public decimal ValorTotal { get; set; }
    }
}
