namespace AplicacaoBackEnd.DTO
{
    // Dados devolvidos no JSON da resposta.
    public class FuncionarioRespostaDTO
    {
        // Dados pessoais.
        public int Idpessoa { get; set; }

        public string Nome { get; set; } = string.Empty;

        public long Cpf { get; set; }

        public DateOnly DataNascimento { get; set; }

        public string? Sexo { get; set; }

        public string? Email { get; set; }

        public long? Telefone { get; set; }

        public string? AtivoPessoa { get; set; }

        public List<EnderecoRespostaDTO> Enderecos { get; set; } = new List<EnderecoRespostaDTO>();

        // Dados profissionais.
        public string Cargo { get; set; } = string.Empty;

        public DateOnly DataAdmissao { get; set; }
    }
}
