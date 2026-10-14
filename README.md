# Aplicação BackEnd — Atividade 03

API ASP.NET Core 8 com Entity Framework Core e Pomelo, organizada com DTOs de entrada e saída. Os três desafios da atividade estão implementados: funcionários/pessoas/endereços, entradas de produtos/estoque e atendimentos com produtos e serviços.

## Preparação

1. Use o banco **db_atendimento** que você já configurou.
2. Confira a conexão em appsettings.json, ou configure a variável ConnectionStrings__connection.
3. Mantenha as três triggers originais fornecidas no SQL: trg_produto_estoque, trg_entrada_produto e trg_atendimentoitem_estoque, além da função fn_calcular_preco_venda.
4. Execute dotnet restore e dotnet run. No ambiente Development, abra /swagger.

A API não recria o banco e não executa os scripts de teste recebidos. As tabelas esperadas são as do SQL utilizado no projeto, incluindo quantidade_item em atendimentoitem e a chave estrangeira entrada_produto → produto.

## Endpoints

| Recurso | Listar | Buscar | Cadastrar | Atualizar | Excluir |
|---|---|---|---|---|---|
| Pessoas e endereços | GET /api/PessoaEndereco/all | GET /api/PessoaEndereco/{id} | POST /api/PessoaEndereco | PUT /api/PessoaEndereco/{id} | DELETE /api/PessoaEndereco/{id} |
| Funcionários completos | GET /api/Funcionario/all | GET /api/Funcionario/{id} | POST /api/Funcionario | PUT /api/Funcionario/{id} | DELETE /api/Funcionario/{id} |
| Categorias | GET /api/Categoriums | GET /api/Categoriums/{id} | POST /api/Categoriums | PUT /api/Categoriums/{id} | DELETE /api/Categoriums/{id} |
| Produtos e serviços | GET /api/Items | GET /api/Items/{id} | POST /api/Items | PUT /api/Items/{id} | DELETE /api/Items/{id} |
| Produtos (JSON simples) | GET /api/ProdutoItem | GET /api/ProdutoItem/{id} | POST /api/ProdutoItem | PUT /api/ProdutoItem/{id} | DELETE /api/ProdutoItem/{id} |
| Entradas de produtos | GET /api/EntradaProduto/all | GET /api/EntradaProduto/{id} | POST /api/EntradaProduto | PUT /api/EntradaProduto/{id} | DELETE /api/EntradaProduto/{id} |
| Atendimentos completos | GET /api/Atendimento/all | GET /api/Atendimento/{id} | POST /api/Atendimento | PUT /api/Atendimento/{id} | DELETE /api/Atendimento/{id} |

Itens de um atendimento também podem ser gerenciados individualmente:

- POST /api/Atendimento/{id}/itens
- PUT /api/Atendimento/{id}/itens/{idItem}
- DELETE /api/Atendimento/{id}/itens/{idItem}

O GET do atendimento retorna cliente, data, status, itens, quantidades, preços praticados, funcionários e total. Um atendimento deve ter pelo menos um item. O mesmo item só pode aparecer uma vez no atendimento, pois sua chave é (idatendimento, iditem).

## Funcionário: POST e PUT completos

O POST cria pessoa, endereços e funcionário na mesma transação. O PUT agora recebe o mesmo conjunto de dados e atualiza as três partes.

~~~json
{
  "nome": "Funcionário Exemplo",
  "cpf": 12345678901,
  "dataNascimento": "2000-01-15",
  "sexo": "M",
  "email": "exemplo@email.com",
  "telefone": 79999999999,
  "ativoPessoa": "S",
  "cargo": "Atendente",
  "dataAdmissao": "2026-10-14",
  "enderecos": [
    { "cep": "49000000", "numero": "10", "complemento": "Casa" }
  ]
}
~~~

No PUT, informe o ID do funcionário na URL. Para cada endereço que já existe, envie idendereco; para um novo, omita o ID ou envie 0. A lista enviada é a lista final: endereços omitidos são removidos. É obrigatório manter pelo menos um endereço.

O DELETE de funcionário remove o vínculo profissional, preserva a pessoa e os endereços e é bloqueado quando existem atendimentos feitos por ele.

## Produto ou serviço pelo Items

Um serviço usa tipoItem S e não recebe produto:

~~~json
{
  "idCategoria": 1,
  "item": "Lavagem simples",
  "descricaoItem": "Serviço de lavagem",
  "preco": 50,
  "tipoItem": "S",
  "ativoProdutoServico": "S"
}
~~~

Um produto usa tipoItem P e recebe os dados específicos em produto:

~~~json
{
  "idCategoria": 1,
  "item": "Shampoo",
  "preco": 20,
  "tipoItem": "P",
  "ativoProdutoServico": "S",
  "produto": { "codigoPreco": 1, "precoVenda": 20.80, "unidade": "UN" }
}
~~~

ProdutoItem continua aceitando o JSON plano usado anteriormente. No PUT não é permitido converter produto em serviço ou vice-versa, evitando inconsistência com estoque e histórico.

## Entrada de produto

~~~json
{
  "idItem": 1,
  "dataEntradaProduto": "2026-10-14",
  "quantidadeEntrada": 10,
  "unidade": "UN",
  "valorUnitarioEntrada": 20
}
~~~

O produto deve existir e possuir estoque. No POST, a trigger soma a entrada e calcula o preço; a API não soma duas vezes.

No PUT, a API aplica a diferença entre a quantidade anterior e a nova. Se mudar o produto, estorna o produto anterior e acrescenta ao novo. O DELETE estorna a entrada. Operações que deixariam saldo negativo retornam 409 e são desfeitas.

Em alterações e exclusões, o custo/preço é recomposto pela entrada de maior ID restante, usando a mesma margem da função SQL: código 1 = 4%, 2 = 8%, 3 = 10%, demais = sem acréscimo. Se não houver outra entrada, mantém o último custo conhecido.

## Atendimento

~~~json
{
  "idPessoa": 1,
  "dataAtendimento": "2026-10-14",
  "statusAtendimento": "A",
  "itens": [
    { "idItem": 1, "idFuncionario": 2, "quantidadeItem": 2, "valorUnitarioEntrada": 26 },
    { "idItem": 3, "idFuncionario": 2, "quantidadeItem": 1, "valorUnitarioEntrada": 50 }
  ]
}
~~~

Troque os IDs pelos registros do seu banco. Pessoas, funcionários e itens devem existir.

O PUT completo recebe a lista final de itens. A API adiciona novos, altera os existentes e remove os omitidos. Produtos movimentam estoque; serviços não. A exclusão do atendimento devolve os produtos ao estoque. A exclusão individual do último item é bloqueada; nesse caso exclua o atendimento inteiro.

## Regras de estoque e transações

- O esquema atual usa INT em quantidade_estoque e quantidade_item. A API aceita apenas quantidades inteiras positivas, inclusive nas entradas, para evitar perda de frações.
- As triggers originais agem apenas no INSERT. Os services fazem as compensações de UPDATE e DELETE. Não instale outras triggers para essas compensações sem adaptar a API, pois isso duplicaria a movimentação.
- Operações compostas usam transação. Uma falha desfaz o cadastro ou a alteração inteira.
- Os saldos são bloqueados com SELECT ... FOR UPDATE, em ordem de produto, durante movimentações da API.
- Alterações diretas no banco fora da API devem respeitar as mesmas regras.

## Organização

- DTO: contratos de entrada e saída; entidades do EF não são expostas diretamente.
- Controllers: rotas, respostas HTTP e limites das transações.
- Services/PessoaService: reutiliza o cadastro e a atualização de pessoa/endereço no funcionário.
- Services/ItemService: regras comuns de produtos e serviços.
- Services/EstoqueService: bloqueio de saldo, limites e recomposição de preço.
- Services/AtendimentoService: sincronização da associação N:N e compensação de estoque.
- Services/ApiExceptionFilter: converte validações, conflitos de chave e erros do banco em respostas da API; detalhes internos ficam no log.

Respostas usuais: 200 consulta, 201 criação, 204 alteração/exclusão, 400 dados inválidos, 404 registro ausente e 409 conflito de estoque ou relacionamento.

## Validação

~~~bash
dotnet build
python tests/integracao_api.py --base-url http://127.0.0.1:5088
~~~

Execute o teste de integração contra uma instância da API ligada a um **banco de teste vazio**, preparado com as tabelas, a função e as três triggers originais. Ele cria cadastros fictícios, testa os CRUDs e remove os registros criados no final. Se uma verificação falhar, ele interrompe e pode deixar esses registros no banco de teste.

O arquivo tests/fixtures/banco_teste.sql prepara esse esquema isolado com o nome atividade_backend_test. Configure a conexão da instância de teste para esse banco antes de rodar o script Python.

O teste cobre cadastro/atualização completa de funcionário, múltiplos endereços, CPF duplicado, produtos/serviços, entradas, preços, troca do produto da entrada, atendimento N:N, itens individuais, PUT repetido, rollback, estorno de entrada já consumida e duas vendas simultâneas.

Validação desta implementação: compilação sem erros ou avisos e 90 requisições HTTP verificadas contra MariaDB 10.11 com as três triggers de INSERT do SQL original.
