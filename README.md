# Aplicação BackEnd + Angular — Atividade 04

API ASP.NET Core 8 com Entity Framework Core e Pomelo, com DTOs para receber os dados e respostas montadas nos controllers. Reúne a loja virtual e o painel administrativo da Estética & Bem-Estar, reutilizando pessoas, itens, estoque e atendimentos da atividade anterior. A conferência dos requisitos RF01–RF24 está em [docs/atividade04.md](docs/atividade04.md).

## Executar o sistema completo no Windows

O backend C# continua na raiz (`Controllers`, `DTO`, `Models`, `Program.cs`). O Angular foi acrescentado somente na pasta `frontend`. São dois processos em desenvolvimento: a API na porta 7067 e a interface na porta 4200.

### 1. Instalações necessárias, uma única vez

Use **Node.js 24 LTS, versão 24.15 ou superior**, e **.NET 8 SDK**. O npm já acompanha o Node.js. O Angular CLI é uma dependência local deste projeto, instalada pelo `npm ci`: não precisa instalar Angular globalmente.

Se ainda não tiver essas ferramentas, instale pelo PowerShell com o WinGet:

```powershell
winget install --exact --id OpenJS.NodeJS.LTS
winget install --exact --id Microsoft.DotNet.SDK.8
```

Feche e abra novamente o terminal após instalar. Confira:

```powershell
node --version
npm --version
dotnet --version
```

Também estão disponíveis os instaladores oficiais: [Node.js](https://nodejs.org/en/download) e [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

### 2. Atualize o projeto e confira o banco

Dentro da pasta do seu clone de `AplicacaoBackEnd`, pare a API antes de atualizar:

```powershell
git pull
```

Quem ainda não clonou pode usar:

```powershell
git clone https://github.com/J-Tabosa/AplicacaoBackEnd.git
cd AplicacaoBackEnd
```

Inicie o seu MySQL/MariaDB e use o **db_atendimento** que já possui. Confira servidor, porta, usuário e senha em `appsettings.json`. A conexão atual usa `localhost:3312`; se o seu banco estiver em outra porta, ajuste esse arquivo. A aplicação não recria o banco.

O banco precisa conter as três triggers originais e a função `fn_calcular_preco_venda`. Uma falha nelas também aparece ao cadastrar pelo Angular: mudar de interface não corrige uma função ausente no banco.

### Atualização do banco para a Atividade 04

No MySQL Workbench, selecione **db_atendimento** e abra `scripts/atividade04.sql`. Execute primeiro a consulta de e-mails duplicados. Se houver resultados, corrija os cadastros repetidos antes de executar o restante do arquivo. O script normaliza os e-mails e cria um índice único; CPF já possui unicidade no esquema original. E-mail vazio continua permitido e é salvo como `NULL`.

Não é necessário recriar o seu banco. A API não aplica esse SQL automaticamente. O índice também impede e-mails repetidos quando dois cadastros chegam juntos.

O painel agora exige identificação de um **funcionário já cadastrado**, com CPF e data de nascimento. Se já existe funcionário, use os dados dele. Somente se não existir nenhum, personalize os dados fictícios de `scripts/primeiro_funcionario.sql` antes de executá-lo no banco selecionado. Esse arquivo cadastra pessoa, endereço e vínculo de funcionário; não precisa usá-lo em um banco que já possui funcionários.

### 3. Terminal 1: iniciar a API

Na raiz do repositório:

```powershell
dotnet restore
dotnet dev-certs https --trust
dotnet run --launch-profile https
```

Aceite a confiança no certificado de desenvolvimento quando o Windows perguntar. Deixe esse terminal aberto. O Swagger fica em **https://localhost:7067/swagger**.

### 4. Terminal 2: iniciar o Angular

Abra outro terminal na mesma raiz do repositório:

```powershell
cd frontend
npm ci
npm start
```

Abra **http://localhost:4200** no navegador. Deixe esse terminal aberto também. Nos próximos dias, basta iniciar o banco e repetir os dois comandos de execução: `dotnet run --launch-profile https` na raiz e `npm start` em `frontend`. Rode `npm ci` novamente quando o `package-lock.json` mudar.

### Como o Angular acessa a API

`frontend/src/app/services/api.config.ts` define o prefixo `/api`. Durante `npm start`, `frontend/proxy.conf.cjs` encaminha essas chamadas para `https://localhost:7067`. O navegador usa somente a porta 4200 para a interface e suas requisições.

Se você mudar a porta da API, altere o destino em `proxy.conf.cjs` e reinicie o Angular. Para usar temporariamente uma API HTTP em outra porta no PowerShell:

```powershell
$env:API_TARGET = "http://localhost:5290"
npm start
```

O destino padrão é HTTPS 7067. `secure: false` no proxy permite que o servidor de desenvolvimento do Angular converse com o certificado local do ASP.NET.

### Loja e identificação

A página inicial é `/loja`: catálogo de produtos e serviços ativos, filtros por nome/categoria/tipo, detalhes por ID e carrinho. O carrinho permite alterar quantidades, remover itens e mantém seu conteúdo ao recarregar a página. O cadastro público inclui pessoa e seus endereços, confere CPF/e-mail repetidos e identifica o cliente após salvar.

Em **Meus pedidos**, a API usa a pessoa da sessão; passar outro ID na URL ou no JSON não permite consultar pedidos de outro cliente. A compra recebe somente IDs dos itens e quantidades. A API consulta preços e saldo novamente, grava o pedido e seus itens e confirma a transação inteira. O pedido começa aberto. O primeiro funcionário ativo por ID é usado como responsável, porque o esquema original exige um funcionário por item; o painel permite editar o responsável enquanto o pedido estiver aberto.

A identificação usa **CPF e data de nascimento, sem senha**, conforme a proposta didática. A sessão fica em cookie HttpOnly; o localStorage guarda somente o carrinho. Funcionários acessam o painel; clientes acessam a loja e os próprios pedidos. Isso é identificação simplificada para a atividade, não autenticação adequada para uma loja pública em produção.

No Swagger, execute `POST /api/Sessao` com `cpf` e `dataNascimento` antes das operações administrativas. O navegador reutiliza o cookie. `GET /api/Sessao` consulta a identificação e `DELETE /api/Sessao` encerra a sessão.

### Telas administrativas disponíveis

- **Pessoas:** cadastro, edição, exclusão e vários endereços, preservando o ID dos endereços existentes.
- **Funcionários:** cadastro e edição da pessoa, dos endereços e dos dados profissionais na mesma operação.
- **Categorias:** cadastro, edição e exclusão, com os conflitos de vínculo informados pela API.
- **Produtos e serviços:** categoria, preço, situação e dados específicos de produto; também mostra o saldo atual.
- **Entradas:** recebimento, alteração e estorno de produtos; atualiza o saldo e os preços pela API.
- **Atendimentos/pedidos:** consulta por ID, filtro por situação, dados do cliente, itens, quantidades, preço praticado, funcionário e cargo, total e atualização de situação.
- **Estoque:** listagem e consulta por ID, com descrição, categoria, preços, unidade, situação e quantidade.
- **Resumo:** contagens reais de clientes/pessoas, funcionários, categorias, produtos, serviços, entradas, pedidos e pedidos abertos, além do total de unidades em estoque.

Para experimentar, identifique-se com um funcionário existente e cadastre uma pessoa, uma categoria, um produto e um serviço. Depois registre uma entrada para o produto e crie um atendimento. Os produtos precisam de saldo; os serviços não movimentam estoque.

### Como ler o frontend

- `src/app/models`: campos que vêm da API ou serão enviados para ela. Os nomes acompanham o JSON existente, incluindo `idpessoa`, `dataNascimento` e `enderecos`.
- `src/app/services`: métodos HTTP de cada cadastro. `listar` usa GET, `adicionar` usa POST, `alterar` usa PUT e `remover` usa DELETE. Os PUTs e DELETEs recebem respostas sem corpo (`void`).
- `src/app/components`: lógica da tela em `.ts` e formulário/tabela em `.html`. Pessoas e funcionários usam o mesmo componente; a configuração da rota indica qual cadastro será exibido.
- `src/app/app.routes.ts`: caminhos do menu e seus componentes.
- `src/styles.css`: estilos compartilhados, adaptados para computador e celular.

Os formulários usam `ngModel`, como o Angular usado de base. `signal` atualiza listas e mensagens depois das consultas. `structuredClone` copia os dados ao editar para que a tabela só mude após salvar. `subscribe` trata tanto o sucesso quanto as mensagens de erro. Datas são enviadas como `AAAA-MM-DD`, preços como números e CEP como texto, preservando os zeros iniciais.

Base utilizada: [AngularFinal](https://github.com/joaovsouza318-gif/AngularFinal). As telas de atletas/corridas/inscrições foram adaptadas para o domínio desta API. Não há chamadas ao MockAPI nem à antiga API da porta 8000.

### Compilar e testar

Na raiz, compile o backend:

```powershell
dotnet build
```

Em `frontend`, compile a interface:

```powershell
npm run build
```

A saída Angular fica em `frontend/dist/atendimento/browser`. `npm start` usa o servidor de desenvolvimento; publicar a interface em um servidor exige configurar o encaminhamento de `/api` para a API e o retorno de `index.html` nas rotas Angular.

Os testes de integração devem usar uma instância da API conectada ao banco de teste `atividade_backend_test`, preparado nesta ordem: `tests/fixtures/banco_teste.sql`, `scripts/atividade04.sql` e `tests/fixtures/administrador_teste.sql`. Selecione `atividade_backend_test` antes dos dois últimos arquivos; o administrador fictício é exclusivo dos testes. Use uma instância de teste da API na porta 5089, com `ASPNETCORE_ENVIRONMENT=Development` e `ConnectionStrings__connection` apontando para esse banco, nunca para seus dados reais. Execute os testes Python já existentes com a API de teste em execução:

```powershell
python tests/integracao_api.py --base-url http://127.0.0.1:5089
python tests/integracao_atividade04.py --base-url http://127.0.0.1:5089
```

Para os testes no navegador, instale o Chromium uma vez e indique o endereço dessa API de teste. O Playwright inicia o Angular automaticamente na porta 4200; feche outra instância do Angular antes de testar um destino diferente:

```powershell
cd frontend
npx playwright install chromium
$env:API_TARGET = "http://127.0.0.1:5089"
npm test
```

Os testes no navegador criam registros fictícios identificados por um código próprio e os removem ao terminar. Cobrem os cadastros, múltiplos endereços, preços, estoque, filtros, carrinho, identificação, compra, privacidade dos pedidos, mudança de situação, tela pequena e API indisponível. A suíte Python da Atividade 04 preserva pedidos concluídos por regra da API: recrie somente o banco isolado antes de repeti-la. Os testes usam CPF `90000000001` e nascimento `1990-01-01` da fixture; as variáveis `API_TEST_ADMIN_CPF` e `API_TEST_ADMIN_NASCIMENTO` permitem usar outro funcionário de teste.

Validação da integração: backend e Angular compilados sem erros; 181 requisições HTTP aprovadas (111 do fluxo anterior e 70 da Atividade 04) e 4 cenários no Chromium aprovados com a API de teste em HTTP, usando um banco MariaDB isolado. A compilação Angular também verifica os tipos nos templates.

## Preparação

1. Use o banco **db_atendimento** que você já configurou.
2. Confira a conexão em appsettings.json, ou configure a variável ConnectionStrings__connection.
3. Mantenha as três triggers originais fornecidas no SQL: trg_produto_estoque, trg_entrada_produto e trg_atendimentoitem_estoque, além da função fn_calcular_preco_venda.
4. Execute `scripts/atividade04.sql` no banco selecionado, conforme explicado acima.
5. Inicie a API e o Angular com os comandos acima. No ambiente Development, abra /swagger.

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

As consultas de categorias e itens ativos e o cadastro de cliente são públicos. As demais rotas da tabela exigem funcionário identificado. Sem sessão, a API responde 401; cliente tentando usar o painel recebe 403.

Rotas acrescentadas para a loja e o estoque:

| Operação | Rota | Acesso |
|---|---|---|
| Identificar / consultar sessão / sair | POST / GET / DELETE `/api/Sessao` | GET exige sessão |
| Catálogo com filtros | GET `/api/Items?nome=creme&idCategoria=1&tipo=P` | Público; somente ativos |
| Estoque completo / por ID | GET `/api/Estoque` e `/api/Estoque/{id}` | Funcionário |
| Confirmar compra | POST `/api/Atendimento/checkout` | Pessoa identificada |
| Próprios pedidos / detalhe | GET `/api/Atendimento/meus` e `/api/Atendimento/meus/{id}` | Dono do pedido |
| Filtrar pedidos | GET `/api/Atendimento/all?status=A` | Funcionário |
| Alterar situação | PUT `/api/Atendimento/{id}/status` | Funcionário |

O filtro de situação aceita A/P/F/C ou seus nomes. Funcionários também podem consultar itens inativos e filtrar `ativo=S` ou `ativo=N`. O catálogo público sempre exclui inativos.

Exemplo de compra: `{"itens":[{"idItem":1,"quantidadeItem":2}]}`. Preços, cliente e responsável são definidos pelo servidor. Exemplo de alteração de situação: `{"statusAtendimento":"P"}`.

Itens de um atendimento **aberto** também podem ser gerenciados individualmente:

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

O PUT completo recebe a lista final de itens. A API adiciona novos, altera os existentes e remove os omitidos. Produtos movimentam estoque; serviços não. A exclusão de um atendimento aberto devolve os produtos ao estoque. A exclusão individual do último item é bloqueada; nesse caso exclua o atendimento inteiro.

### Situação do pedido

O fluxo permitido é **A (Aberto) → P (Em processamento) → F (Concluído)**. É possível cancelar (C) a partir de A ou P. Use a rota de situação para isso; o PUT completo não muda a situação.

Somente pedidos abertos permitem editar cliente/itens/responsáveis. Pedidos em processamento ou concluídos não podem ser excluídos. Cancelar devolve os produtos ao estoque uma única vez e mantém os itens e os preços no histórico. Repetir a mesma situação não duplica a movimentação. Excluir posteriormente um cancelado não devolve estoque outra vez. Serviços não movimentam estoque.

## Regras de estoque e transações

- O esquema atual usa INT em quantidade_estoque e quantidade_item. A API aceita apenas quantidades inteiras positivas, inclusive nas entradas, para evitar perda de frações.
- As triggers originais agem apenas no INSERT. Os controllers fazem as compensações de UPDATE e DELETE. Não instale outras triggers para essas compensações sem adaptar a API, pois isso duplicaria a movimentação.
- Operações compostas usam transação. Uma falha desfaz o cadastro ou a alteração inteira.
- Os saldos são bloqueados com SELECT ... FOR UPDATE, em ordem de produto, durante movimentações da API.
- Alterações diretas no banco fora da API devem respeitar as mesmas regras.

## Organização e como ler o código

As rotas, consultas, validações e gravações ficam nos próprios controllers. Não há pasta Services nem DTO herdando de outro DTO.

- `Models`: classes que representam as tabelas, geradas a partir do banco.
- `DTO`: classes com os campos do JSON. Cada classe fica em um arquivo com o mesmo nome.
- DTOs como `FuncionarioCadastroDTO` recebem os dados enviados no POST/PUT.
- Os controllers montam as respostas com `new { ... }`, escolhendo os campos devolvidos no JSON. Não há DTOs exclusivos de resposta. `ProdutoDadosDTO` recebe a parte `produto` do item.
- `Controllers`: recebem a requisição, conferem os dados, usam `_context` para acessar o banco e devolvem a resposta HTTP.

O `CategoriumsController` e o `CategoriaDTO` têm comentários explicando cada parte do CRUD e das validações.

Para acompanhar um cadastro de funcionário, abra `FuncionarioController.cs` e procure `PostFuncionario`. Ele confere os dados, monta a pessoa com seus endereços, salva para obter o ID, cria o funcionário com esse ID e confirma a transação. `PutFuncionario` atualiza essas três partes no mesmo método.

Métodos privados como `ExibirFuncionario` apenas montam o objeto devolvido no JSON. No `AtendimentoController`, `AtualizarItens` concentra a regra repetida nas rotas do atendimento e dos itens. Ele também fica no controller: retorna uma resposta de erro quando há problema e `null` quando a atualização deu certo. Isso evita escrever a mesma movimentação de estoque em cada rota.

O nome `builder.Services` em `Program.cs` pertence à configuração do ASP.NET. Ele registra o contexto do banco e os recursos da API; não representa uma camada de services criada no projeto.

### Termos que aparecem nos DTOs e controllers

| Código | Significado |
|---|---|
| `{ get; set; }` | Permite ler e preencher a propriedade. |
| `string.Empty` | Texto vazio usado como valor inicial. |
| `?`, como em `DateOnly?` | O campo pode estar sem valor. Com `[Required]`, uma requisição sem esse campo é recusada. |
| `!` depois de uma referência, como `p!` | Avisa ao compilador que, naquele trecho, a referência será tratada como preenchida. Não é o mesmo que `!=` ou negar uma condição. |
| `new List<EnderecoDTO>()` | Cria uma lista vazia de endereços. |
| `[Required]` | Campo obrigatório. |
| `[StringLength(100)]` | Limita o tamanho do texto. |
| `[Range(0.0, 99999999.99)]` | Define o intervalo numérico permitido, sem converter limites escritos como texto. |
| `[RegularExpression("^[SN]$")]` | Aceita somente S ou N. A validação de P/S segue a mesma ideia. |
| `async`, `Task` e `await` | Permitem esperar uma consulta ou gravação no banco sem ocupar a execução enquanto ela termina. |
| `Include` / `ThenInclude` | Carregam os registros relacionados, como funcionário → pessoa → endereços. |
| `AsNoTracking` | Indica uma consulta para leitura, sem acompanhar os dados para atualização. |
| `OrderBy` | Ordena os resultados. |
| `foreach` | Percorre uma lista para montar a resposta ou atualizar cada registro. |
| `Any` / `FirstOrDefault` | Verifica se existe um registro / procura o primeiro, retornando `null` se não houver. |
| `SaveChangesAsync` | Envia as alterações para o banco. Dentro de uma transação, ainda podem ser desfeitas. |
| `CommitAsync` / `RollbackAsync` | Confirma / desfaz a transação. Sair de um `await using` sem confirmar também desfaz as alterações. |
| `FOR UPDATE` | Bloqueia temporariamente o registro durante a transação para evitar que duas operações usem o mesmo saldo ao mesmo tempo. |
| `FromSqlInterpolated` | Executa a consulta SQL parametrizada; os valores entre chaves são enviados como parâmetros. |

As verificações que dependem de outros campos ou do banco usam `if` no controller. Os DTOs não implementam interfaces de validação e não usam `yield`.

Respostas usuais: 200 consulta, 201 criação, 204 alteração/exclusão, 400 dados inválidos, 401 sem identificação, 403 sem permissão, 404 registro ausente e 409 conflito de estoque ou relacionamento. Cada gravação trata `DbUpdateException` no próprio controller e registra o detalhe técnico no log.

