# Senhas e imagens dos produtos

## Atualizar o banco existente

Pare a API, faça o pull e execute [scripts/senha_imagem_produto.sql](../scripts/senha_imagem_produto.sql) com **db_atendimento** selecionado. O script pode ser executado novamente: verifica a coluna e usa CREATE TABLE IF NOT EXISTS. Não recria o banco nem apaga cadastros.

Ele acrescenta `pessoa.senha_hash VARCHAR(512) NULL` e cria a tabela `imagem_produto`. Cliente e funcionário compartilham a pessoa; por isso a senha fica em `pessoa`, uma única vez.

Os registros antigos começam com senha NULL e não conseguem entrar até que uma senha seja definida. O cadastro público não permite tomar posse de um CPF existente para definir senha.

## Definir a primeira senha

Na raiz do repositório, com a conexão correta em `appsettings.json`:

```powershell
dotnet run --no-launch-profile -- --definir-senha
```

Informe CPF, senha e confirmação no terminal. A senha não aparece enquanto é digitada. O comando usa o mesmo PasswordHasher da API e encerra após salvar. Também aceita o CPF como argumento: `dotnet run --no-launch-profile -- --definir-senha 90000000001` (substitua pelo CPF desejado). A senha não é passada como argumento.

Depois que um funcionário tiver senha, ele pode entrar no painel e definir/alterar senhas pelo formulário de Pessoas ou Funcionários. Novos clientes definem a própria senha no cadastro público. Não existe senha padrão para os cadastros reais. O script opcional de primeiro funcionário também precisa desse comando após inserir a pessoa.

## Como a senha funciona

O C# usa `Microsoft.AspNetCore.Identity.PasswordHasher<Pessoa>`, com PBKDF2, salt aleatório e 210.000 iterações. No cadastro, `HashPassword` gera o texto que será salvo; no login, `VerifyHashedPassword` confere a senha recebida. O framework inclui no hash as informações necessárias para verificá-lo e permite atualizar hashes antigos no próximo login.

Hash de senha não é criptografia reversível: não existe uma operação para recuperar a senha original. Não use MD5 ou SHA diretamente no SQL para preencher `senha_hash`. Duas pessoas com a mesma senha terão hashes diferentes por causa do salt.

A classe `Seguranca/Senhas.cs` usa SHA-256 **somente para gerar a marca da versão da senha no cookie**, a partir do hash já pronto. O hash de autenticação continua sendo o PasswordHasher/PBKDF2. Trocar a senha muda essa marca e invalida as sessões antigas. Remover o vínculo de funcionário ou desativar a pessoa também invalida o acesso correspondente.

O campo `Senha` entra no DTO, mas `SenhaHash` não sai nas respostas. A senha não é guardada no localStorage; a sessão usa cookie HttpOnly e o carrinho continua sendo o único estado persistido ali. Cookies da versão antiga deixam de funcionar após a atualização.

No POST de pessoa/cliente/funcionário, a senha é obrigatória, com 8 a 128 caracteres. No PUT, omita `senha` ou envie `null` para manter a atual; envie uma senha válida para trocá-la. String vazia enviada diretamente à API é inválida. O formulário trata os campos vazios como manter a senha atual.

## Login

`POST /api/Sessao` recebe:

```json
{
  "login": "cliente@example.com",
  "senha": "MinhaSenha123"
}
```

Também aceita CPF no campo login, como `"90000000001"` ou com pontuação. A API normaliza o e-mail; a senha é comparada exatamente, sem remover espaços. A data de nascimento continua sendo um dado cadastral, mas deixou de ser credencial de login.

Pessoa inexistente, inativa, sem senha ou com senha errada recebe a mesma resposta 401. Há um limite padrão de dez tentativas por minuto por IP; exceder responde 429. A configuração `Login:LimitePorMinuto` permite ajustar esse limite (nas suítes de teste, use um valor maior para não bloquear as verificações automatizadas).

O retorno de sucesso continua sendo `{ "idpessoa": 1, "nome": "...", "funcionario": true }`. O perfil é obtido do banco, não do JSON enviado pelo navegador.

## Imagem do produto

A tabela `imagem_produto` guarda:

| Campo           | Conteúdo                                                           |
| --------------- | ------------------------------------------------------------------ |
| `iditem`        | Chave primária e FK para `produto.iditem`: uma imagem por produto. |
| `nome_arquivo`  | Nome do arquivo, sem caminho de armazenamento.                     |
| `tipo_conteudo` | MIME detectado: image/png, image/jpeg ou image/webp.               |
| `dados`         | Bytes da imagem em MEDIUMBLOB.                                     |
| `versao`        | Identificador alterado a cada substituição da foto.                |
| `atualizado_em` | Data/hora UTC da atualização.                                      |

O BLOB comum é pequeno para fotos; MEDIUMBLOB comporta os arquivos aceitos. A API limita o upload a **2 MiB**, verifica tamanho, extensão, MIME e assinatura binária e não aceita SVG/HTML. O banco usa ON DELETE CASCADE para apagar a foto se o produto for excluído.

| Método e rota                   | Comportamento                                                                                                                     |
| ------------------------------- | --------------------------------------------------------------------------------------------------------------------------------- |
| GET `/api/Items/{id}/imagem`    | Devolve os bytes com o Content-Type correto. O público só consulta imagens de itens ativos; funcionário também consulta inativos. |
| PUT `/api/Items/{id}/imagem`    | Funcionário envia ou substitui a foto com multipart/form-data, campo `arquivo`.                                                   |
| DELETE `/api/Items/{id}/imagem` | Funcionário remove a foto; repetir a remoção retorna 204.                                                                         |

`GET /api/Items`, `GET /api/Items/{id}` e as consultas de `ProdutoItem` retornam `imagemUrl` ou null. O JSON não contém o BLOB. A consulta de catálogo carrega apenas metadados da imagem; os bytes são buscados na rota de arquivo quando a tela precisa exibi-la. Sem foto, a loja mantém a ilustração genérica existente.

## Formulário do produto

No painel, cadastre ou edite um item do tipo Produto e escolha o arquivo em **Imagem do produto**. A prévia é local; os bytes só são enviados quando você salva. Depois de salvar, a foto aparece nos cards da loja, no detalhe e na listagem administrativa.

O item e a foto usam duas requisições. Se salvar o item funcionar e o upload falhar, o formulário informa essa situação e mantém o ID do produto. Você pode tentar novamente sem cadastrar outro item. Substituição e remoção usam a mesma tabela/linha; serviços não recebem imagem de produto.

## Validação automatizada

A suíte `tests/integracao_senha_imagem.py` cobre exigência de senha, login por CPF/e-mail, tentativa de login antiga, troca de senha com revogação da sessão, edição sem troca de senha, perfil administrativo, hash ausente no JSON, formatos de imagem, bloqueio de cliente/visitante para upload, arquivos inválidos/grandes, substituição, remoção e produto desativado.

O cenário `frontend/e2e/senha-imagem.spec.ts` verifica também os formulários, foto no catálogo/detalhe e recuperação de uma falha no upload sem duplicar o produto. Use somente banco isolado para executar as suítes, conforme o README.

Referências da implementação: [PasswordHasher do ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-configuration?view=aspnetcore-8.0#password-hasher-options) e [uploads no ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads?view=aspnetcore-8.0).
