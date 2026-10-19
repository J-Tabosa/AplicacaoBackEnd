// Disponibiliza os atributos de validação usados abaixo.
using System.ComponentModel.DataAnnotations;

// Agrupa as classes de entrada da API.
namespace AplicacaoBackEnd.DTO
{
    // Recebe o JSON do POST e do PUT de categorias.
    // Exemplo: { "categoria": "Produtos de limpeza" }
    // O ID é gerado pelo banco no POST e informado na URL no PUT.
    public class CategoriaDTO
    {
        // Torna o nome obrigatório: não aceita null, texto vazio ou só espaços.
        [Required]

        // Limita o nome a 35 caracteres, como a coluna do banco.
        [StringLength(35)]

        // string é o tipo texto; Categoria é o nome da propriedade.
        // get permite ler o valor e set permite preenchê-lo.
        // string.Empty define um texto vazio como valor inicial.
        // Ao receber o JSON, o ASP.NET preenche essa propriedade com o valor enviado.
        public string Categoria { get; set; } = string.Empty;
    }
}
