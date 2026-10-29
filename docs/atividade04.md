# Conferência da Atividade 04

A atividade pede dois módulos Angular integrados à API e ao MySQL: loja virtual e painel administrativo da Estética & Bem-Estar. Os nomes de endpoints do enunciado são sugestões; o projeto mantém `Items`, `PessoaEndereco` e `Atendimento`, aproveitando o banco anterior.

## O que já existia e o que foi acrescentado

Já existiam os CRUDs de pessoas com endereços, funcionários completos, categorias, produtos/serviços, entradas e atendimentos. Também existiam as transações, a baixa de estoque na venda e as compensações de alteração/exclusão. O Angular já oferecia essas telas administrativas.

Foram acrescentados a loja, os filtros e detalhes públicos, cadastro do cliente, carrinho, checkout, histórico pessoal, identificação, controle de acesso administrativo, fluxo de situações, consulta de estoque e consultas por ID nas telas. O resumo administrativo passou a separar produtos, serviços, pedidos abertos e quantidade em estoque. E-mail informado também passa a ser único.

## Requisitos e onde conferir

| Requisito | Implementação / como demonstrar |
|---|---|
| RF01 — Catálogo | `/loja` lista somente produtos e serviços ativos vindos da API. |
| RF02 — Detalhes | Clique no nome do item: `/loja/itens/:id` consulta a API pelo ID. Inexistente/inativo retorna 404 ao público. |
| RF03 — Pesquisa | Pesquise nome, categoria e tipo; os botões de categoria alteram os filtros da loja. |
| RF04 — Cadastro do cliente | `/loja/cadastro` cadastra pessoa e endereços. A API valida campos, CPF e e-mail duplicados; o índice protege a unicidade do e-mail. |
| RF05 — Carrinho | Adicione produtos e serviços pela loja ou pelos detalhes. |
| RF06 — Manutenção do carrinho | Altere quantidades, remova itens e confira subtotais/total. Quantidade inválida bloqueia a confirmação. |
| RF07 — Finalização | `POST /api/Atendimento/checkout` grava atendimento e itens na mesma transação. A API define o cliente pela sessão e consulta preços atuais no banco. |
| RF08 — Histórico pessoal | `/loja/pedidos` e `/loja/pedidos/:id` consultam somente os pedidos da pessoa identificada. Tentar ID de outro cliente retorna 404. |
| RF09 — Clientes | Painel → Pessoas: lista, consulta por ID e dados com endereços. |
| RF10 — Produtos | Painel → Produtos e serviços, filtro P: cadastrar, consultar ID, editar e desativar. |
| RF11 — Serviços | Mesma tela, filtro S: CRUD e desativação; serviços não possuem estoque. |
| RF12 — Categorias | Painel → Categorias: lista, ID, cadastro, edição e exclusão. Categoria vinculada retorna conflito. |
| RF13 — Todos os pedidos | Painel → Atendimentos: lista e ID, cliente, endereços, itens, funcionário/cargo, quantidades, preços praticados e total. |
| RF14 — Filtro por situação | Selecione Aberto, Em processamento, Concluído ou Cancelado na lista administrativa. |
| RF15 — Atualização da situação | Botões Processar/Concluir/Cancelar usam `PUT /api/Atendimento/{id}/status`; a API valida as transições. |
| RF16 — Entrada de produtos | Painel → Entradas: produto, data, quantidade, unidade e custo unitário. |
| RF17 — Crédito de estoque | O INSERT usa a trigger original. Alteração/exclusão compensam somente a diferença, dentro de transação. |
| RF18 — Consulta de estoque | Painel → Estoque: lista e ID com item, descrição, categoria, preços base/venda, unidade, quantidade e situação. |
| RF19 — Saída de estoque | Checkout verifica disponibilidade e bloqueia saldo antes de gravar. A trigger debita produtos; serviços não são debitados. |
| RF20 — Feedback | Telas exibem carregamento, sucesso, ausência de dados e mensagens dos erros da API. |
| RF21 — Validações | Formulários mostram os campos inválidos; DTOs/controllers conferem os dados novamente. Não basta validar apenas no Angular. |
| RF22 — Comunicação com a API | Serviços Angular usam HttpClient e tratam consulta, criação, alteração, exclusão, 404, 409 e indisponibilidade da rede/API. |
| RF23 — Navegação dos módulos | Cabeçalho permite navegar entre Loja virtual e Painel administrativo. O painel exige funcionário identificado. |
| RF24 — Resumo administrativo | `/inicio` usa dados reais: pessoas/clientes, funcionários, categorias, produtos, serviços, entradas, pedidos, abertos e unidades em estoque. |

Os layouts do enunciado servem de referência. A interface mantém o estilo do projeto, com adaptação para celular. Não foram acrescentados pagamento, frete ou integração externa, que não fazem parte desse fluxo.

## Regras que ajudam a explicar o código

- Pessoa continua sendo o cadastro base. Funcionário é uma pessoa com vínculo profissional. O cliente da compra é a pessoa identificada, não um ID livre recebido no JSON.
- A sessão usa CPF e nascimento, sem senha, para atender à identificação simplificada da atividade. `SessaoController` cria o cookie; `[Authorize]` verifica a sessão e `[Authorize(Roles = "Funcionario")]` restringe o painel. Isso não substitui autenticação de produção.
- O carrinho é temporário no navegador. Só vira registro no banco ao confirmar a compra. O backend consulta preço, situação e disponibilidade novamente.
- Não há novas tabelas de pedido: `atendimento` é o pedido e `atendimentoitem` guarda suas linhas. O preço praticado fica na linha e não muda quando o preço do catálogo mudar.
- O esquema exige funcionário por item. O checkout escolhe o primeiro funcionário ativo por ID como responsável inicial. O painel pode alterar esse responsável enquanto o pedido estiver aberto.
- Situações: A → P → F; cancelamento C é permitido a partir de A ou P. Cancelamento preserva o histórico e devolve estoque uma vez. Concluído não volta para aberto.
- As regras do C# continuam nos controllers, com DTOs de entrada e respostas anônimas. Os services da pasta `frontend` apenas organizam chamadas HTTP e estado da interface Angular.

## Preparação e validação

Siga o [README](../README.md) para atualizar o índice de e-mail no banco existente, identificar um funcionário e executar os dois projetos. Os três gatilhos originais de INSERT e a função de preço precisam continuar no banco.

Validação realizada em banco MariaDB 10.11 isolado, com a cultura pt-BR: build .NET, build Angular, 111 requisições do fluxo anterior, 70 requisições específicas da Atividade 04 e quatro cenários de navegador. Os testes cobrem também tentativa de consultar pedido alheio, preço adulterado, compra sem saldo, concorrência e cancelamento repetido.

As verificações automatizadas usam dados fictícios. O SQL não foi executado no banco local do aluno; `scripts/atividade04.sql` deve ser aplicado nesse banco antes da demonstração.
