import { expect, test, Page } from '@playwright/test';

test('cadastros completos, atendimento e consistência do estoque pela interface', async ({
  page,
}) => {
  const request = page.request;
  await request.post('/api/Sessao', { data: { cpf: 90000000001, dataNascimento: '1990-01-01' } });
  const codigo = String(Date.now()).slice(-8);
  const categoriaNome = `Categoria E2E ${codigo}`;
  const clienteNome = `Cliente E2E ${codigo}`;
  const funcionarioNome = `Equipe E2E ${codigo}`;
  const produtoNome = `Produto E2E ${codigo}`;
  const servicoNome = `Servico E2E ${codigo}`;
  const cpfCliente = `810${codigo}`;
  const cpfFuncionario = `820${codigo}`;
  const errosPagina: string[] = [];
  page.on('pageerror', (erro) => errosPagina.push(erro.message));
  page.on('dialog', (dialog) => dialog.accept());

  async function navegar(nome: string) {
    await page.getByRole('navigation').getByRole('link', { name: nome, exact: true }).click();
    await expect(page.getByText(/Carregando/)).toHaveCount(0);
  }

  async function estoque(id: number, quantidade: number) {
    await expect
      .poll(async () => {
        const resposta = await request.get(`/api/Items/${id}`);
        expect(resposta.ok()).toBeTruthy();
        return (await resposta.json()).produto.quantidadeEstoque;
      })
      .toBe(quantidade);
  }

  async function preencherPessoa(p: Page, nome: string, cpf: string) {
    await p.getByLabel('Nome completo').fill(nome);
    await p.getByLabel('CPF', { exact: false }).fill(cpf);
    await p.getByLabel('Data de nascimento').fill('2000-01-15');
    await p.getByLabel('E-mail').fill(cpf + '@example.com');
    await p.locator('#cep0').fill('01001000');
    await p.locator('#numero0').fill('10');
  }

  try {
    await page.goto('/inicio');
    await expect(page.getByRole('heading', { name: 'Bem-vindo ao Atendimento' })).toBeVisible();
    await expect(page.locator('.resumo .cartao')).toHaveCount(9);

    await navegar('Categorias');
    await page.getByRole('button', { name: 'Nova categoria' }).click();
    await page.getByLabel('Nome da categoria').fill(categoriaNome);
    await page.getByRole('button', { name: 'Salvar categoria' }).click();
    await expect(page.getByRole('row').filter({ hasText: categoriaNome })).toBeVisible();
    const categoria = (await (await request.get('/api/Categoriums')).json()).find(
      (c: any) => c.categoria === categoriaNome,
    );

    await navegar('Pessoas');
    await page.getByRole('button', { name: 'Nova pessoa' }).click();
    await preencherPessoa(page, clienteNome, cpfCliente);
    await page.getByRole('button', { name: 'Adicionar endereço' }).click();
    await page.locator('#cep1').fill('49000000');
    await page.locator('#numero1').fill('20');
    await page.getByRole('button', { name: 'Salvar cadastro' }).click();
    await expect(page.getByRole('row').filter({ hasText: clienteNome })).toBeVisible();
    const cliente = (await (await request.get('/api/PessoaEndereco/all')).json()).find(
      (p: any) => p.nome === clienteNome,
    );
    expect(cliente.enderecos).toHaveLength(2);
    expect(cliente.enderecos[0].cep).toBe('01001000');

    await page
      .getByRole('row')
      .filter({ hasText: clienteNome })
      .getByRole('button', { name: 'Editar' })
      .click();
    await page.locator('#complemento0').fill('Endereço atualizado');
    await page.getByRole('button', { name: 'Remover endereço' }).nth(1).click();
    await page.getByRole('button', { name: 'Adicionar endereço' }).click();
    await page.locator('#cep1').fill('49000111');
    await page.getByRole('button', { name: 'Salvar cadastro' }).click();
    await expect(page.getByRole('status')).toContainText('Cadastro salvo');
    const clienteAtual = await (
      await request.get(`/api/PessoaEndereco/${cliente.idpessoa}`)
    ).json();
    expect(clienteAtual.enderecos).toHaveLength(2);
    expect(clienteAtual.enderecos[0].idendereco).toBe(cliente.enderecos[0].idendereco);
    expect(clienteAtual.enderecos[1].idendereco).not.toBe(cliente.enderecos[1].idendereco);

    await navegar('Funcionários');
    await page.getByRole('button', { name: 'Novo funcionário' }).click();
    await preencherPessoa(page, funcionarioNome, cpfFuncionario);
    await page.getByLabel('Cargo').fill('Atendente');
    await page.getByLabel('Data de admissão').fill('2026-10-01');
    await page.getByRole('button', { name: 'Salvar cadastro' }).click();
    await expect(page.getByRole('row').filter({ hasText: funcionarioNome })).toBeVisible();
    const funcionario = (await (await request.get('/api/Funcionario/all')).json()).find(
      (p: any) => p.nome === funcionarioNome,
    );
    await page
      .getByRole('row')
      .filter({ hasText: funcionarioNome })
      .getByRole('button', { name: 'Editar' })
      .click();
    await page.getByLabel('Cargo').fill('Supervisor');
    await page.getByRole('button', { name: 'Salvar cadastro' }).click();
    await expect(page.getByRole('row').filter({ hasText: funcionarioNome })).toContainText(
      'Supervisor',
    );

    await navegar('Produtos e serviços');
    await page.getByRole('button', { name: 'Novo item' }).click();
    await page.getByLabel('Nome do item').fill(produtoNome);
    await page.locator('#categoria').selectOption({ label: categoriaNome });
    await page.locator('#tipo').selectOption('P');
    await page.getByLabel('Custo / preço base').fill('20');
    await page.getByLabel('Código de preço').selectOption({ label: '1 · Acréscimo de 4%' });
    await page.getByLabel('Preço de venda').fill('20.80');
    await page.getByRole('button', { name: 'Salvar item' }).click();
    await expect(page.getByRole('row').filter({ hasText: produtoNome })).toBeVisible();
    const produto = (await (await request.get('/api/Items')).json()).find(
      (i: any) => i.item === produtoNome,
    );
    await estoque(produto.idItem, 0);
    await page.getByRole('button', { name: 'Novo item' }).click();
    await page.getByLabel('Nome do item').fill(servicoNome);
    await page.locator('#categoria').selectOption({ label: categoriaNome });
    await page.getByLabel('Preço do serviço').fill('50');
    await page.getByRole('button', { name: 'Salvar item' }).click();
    await expect(page.getByRole('row').filter({ hasText: servicoNome })).toBeVisible();
    const servico = (await (await request.get('/api/Items')).json()).find(
      (i: any) => i.item === servicoNome,
    );
    expect(servico.produto).toBeNull();

    await navegar('Entradas');
    await page.getByRole('button', { name: 'Nova entrada' }).click();
    await page
      .getByLabel('Produto', { exact: false })
      .selectOption({ label: `${produtoNome} · Saldo: 0` });
    await page.getByLabel('Quantidade').fill('10');
    await page.getByLabel('Custo unitário').fill('22');
    await page.getByRole('button', { name: 'Salvar entrada' }).click();
    await expect(page.getByRole('row').filter({ hasText: produtoNome })).toBeVisible();
    await estoque(produto.idItem, 10);
    expect(
      (await (await request.get(`/api/Items/${produto.idItem}`)).json()).produto.precoVenda,
    ).toBe(22.88);
    await page
      .getByRole('row')
      .filter({ hasText: produtoNome })
      .getByRole('button', { name: 'Editar' })
      .click();
    await page.getByLabel('Quantidade').fill('12');
    await page.getByRole('button', { name: 'Salvar entrada' }).click();
    await expect(page.getByRole('status')).toContainText('Entrada salva');
    await estoque(produto.idItem, 12);

    await navegar('Atendimentos');
    await page.getByRole('button', { name: 'Novo atendimento' }).click();
    await page.getByLabel('Cliente').selectOption({ label: clienteNome });
    await page
      .locator('#item0')
      .selectOption(
        (await page
          .locator('#item0 option')
          .filter({ hasText: produtoNome })
          .getAttribute('value')) || '',
      );
    await page.locator('#funcionario0').selectOption({ label: funcionarioNome });
    await page.locator('#qtd0').fill('2');
    await page.locator('#preco0').fill('30');
    await page.getByRole('button', { name: 'Adicionar item' }).click();
    await page
      .locator('#item1')
      .selectOption(
        (await page
          .locator('#item1 option')
          .filter({ hasText: servicoNome })
          .getAttribute('value')) || '',
      );
    await page.locator('#funcionario1').selectOption({ label: funcionarioNome });
    await page.getByRole('button', { name: 'Salvar atendimento' }).click();
    await expect(page.getByRole('row').filter({ hasText: clienteNome })).toBeVisible();
    await estoque(produto.idItem, 10);
    let atendimento = (await (await request.get('/api/Atendimento/all')).json()).find(
      (a: any) => a.idPessoa === cliente.idpessoa,
    );
    expect(atendimento.valorTotal).toBe(110);
    await page
      .getByRole('row')
      .filter({ hasText: clienteNome })
      .getByRole('button', { name: 'Detalhes' })
      .click();
    await expect(page.locator('.detalhes')).toContainText(funcionarioNome);
    await expect(page.locator('.detalhes')).toContainText('01001000');
    await expect(page.locator('.detalhes tbody tr')).toHaveCount(2);
    await page.screenshot({ path: 'test-results/atendimento-desktop.png', fullPage: true });
    await page.getByRole('button', { name: 'Fechar detalhes' }).click();

    await page
      .getByRole('row')
      .filter({ hasText: clienteNome })
      .getByRole('button', { name: 'Editar' })
      .click();
    await page.locator('#qtd0').fill('3');
    await page.locator('#qtd1').fill('2');
    await page.locator('#preco1').fill('40');
    await page.getByRole('button', { name: 'Salvar atendimento' }).click();
    await expect(page.getByRole('status')).toContainText('Atendimento salvo');
    await estoque(produto.idItem, 9);
    atendimento = await (await request.get(`/api/Atendimento/${atendimento.idAtendimento}`)).json();
    expect(atendimento.valorTotal).toBe(170);
    await page
      .getByRole('row')
      .filter({ hasText: clienteNome })
      .getByRole('button', { name: 'Editar' })
      .click();
    await page.getByRole('button', { name: 'Salvar atendimento' }).click();
    await expect(page.getByRole('status')).toContainText('Atendimento salvo');
    await estoque(produto.idItem, 9);

    await navegar('Entradas');
    await page
      .getByRole('row')
      .filter({ hasText: produtoNome })
      .getByRole('button', { name: 'Excluir' })
      .click();
    await expect(page.getByRole('alert')).toContainText('Não é possível estornar');
    await estoque(produto.idItem, 9);
    await navegar('Atendimentos');
    await page
      .getByRole('row')
      .filter({ hasText: clienteNome })
      .getByRole('button', { name: 'Editar' })
      .click();
    await page.locator('#qtd0').fill('1000');
    await page.getByRole('button', { name: 'Salvar atendimento' }).click();
    await expect(page.getByRole('alert')).toContainText('Estoque insuficiente');
    await expect(page.locator('#qtd0')).toHaveValue('1000');
    await estoque(produto.idItem, 9);
    await page.getByRole('button', { name: 'Cancelar', exact: true }).click();
    await page
      .getByRole('row')
      .filter({ hasText: clienteNome })
      .getByRole('button', { name: 'Excluir' })
      .click();
    await expect(page.getByRole('status')).toContainText('Atendimento excluído');
    await estoque(produto.idItem, 12);

    await navegar('Entradas');
    await page
      .getByRole('row')
      .filter({ hasText: produtoNome })
      .getByRole('button', { name: 'Excluir' })
      .click();
    await expect(page.getByRole('status')).toContainText('Entrada excluída');
    await estoque(produto.idItem, 0);
    await navegar('Produtos e serviços');
    for (const nome of [produtoNome, servicoNome]) {
      await page
        .getByRole('row')
        .filter({ hasText: nome })
        .getByRole('button', { name: 'Excluir' })
        .click();
      await expect(page.getByRole('row').filter({ hasText: nome })).toHaveCount(0);
    }
    await navegar('Categorias');
    await page
      .getByRole('row')
      .filter({ hasText: categoriaNome })
      .getByRole('button', { name: 'Editar' })
      .click();
    await page.getByLabel('Nome da categoria').fill(categoriaNome + ' editada');
    await page.getByRole('button', { name: 'Salvar categoria' }).click();
    await expect(page.getByRole('row').filter({ hasText: categoriaNome })).toContainText('editada');
    await page
      .getByRole('row')
      .filter({ hasText: categoriaNome })
      .getByRole('button', { name: 'Excluir' })
      .click();
    await expect(page.getByRole('row').filter({ hasText: categoriaNome })).toHaveCount(0);
    await navegar('Funcionários');
    await page
      .getByRole('row')
      .filter({ hasText: funcionarioNome })
      .getByRole('button', { name: 'Excluir' })
      .click();
    await expect(page.getByRole('row').filter({ hasText: funcionarioNome })).toHaveCount(0);
    expect((await request.get(`/api/PessoaEndereco/${funcionario.idpessoa}`)).ok()).toBeTruthy();
    await navegar('Pessoas');
    for (const nome of [clienteNome, funcionarioNome]) {
      await page
        .getByRole('row')
        .filter({ hasText: nome })
        .getByRole('button', { name: 'Excluir' })
        .click();
      await expect(page.getByRole('row').filter({ hasText: nome })).toHaveCount(0);
    }
    expect(errosPagina).toEqual([]);
  } finally {
    // Limpa exclusivamente os registros identificados pelo código deste teste.
    for (const [rota, id, nome] of [
      ['Atendimento', 'idAtendimento', 'cliente'],
      ['EntradaProduto', 'idEntradaProduto', 'produto'],
      ['Items', 'idItem', 'item'],
      ['Categoriums', 'idcategoria', 'categoria'],
      ['Funcionario', 'idpessoa', 'nome'],
      ['PessoaEndereco', 'idpessoa', 'nome'],
    ]) {
      const sufixo = ['Atendimento', 'EntradaProduto', 'Funcionario', 'PessoaEndereco'].includes(
        rota,
      )
        ? '/all'
        : '';
      const resposta = await request.get(`/api/${rota}${sufixo}`);
      if (resposta.ok()) {
        for (const registro of await resposta.json()) {
          if (String(registro[nome]).includes(codigo)) {
            await request.delete(`/api/${rota}/${registro[id]}`);
          }
        }
      }
    }
  }
});

test('menu e formulários funcionam em tela pequena', async ({ page }) => {
  await page.request.post('/api/Sessao', {
    data: { cpf: 90000000001, dataNascimento: '1990-01-01' },
  });
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto('/pessoas');
  await page.getByRole('button', { name: 'Nova pessoa' }).click();
  await expect(page.getByLabel('Nome completo')).toBeVisible();
  await expect(page.getByRole('button', { name: 'Salvar cadastro' })).toBeDisabled();
  await page.screenshot({ path: 'test-results/pessoas-mobile.png', fullPage: true });
  expect(
    await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth),
  ).toBeTruthy();
  await page.getByRole('button', { name: 'Cancelar' }).click();
  await page
    .getByRole('navigation')
    .getByRole('link', { name: 'Atendimentos', exact: true })
    .click();
  await expect(page.getByRole('heading', { name: 'Atendimentos', exact: true })).toBeVisible();
});

test('mostra a falha da API sem exibir um resumo com contagens falsas', async ({ page }) => {
  await page.route('**/api/**', (route) => route.abort());
  await page.goto('/loja');
  await expect(page.getByRole('alert')).toContainText('Não foi possível acessar a API');
  await expect(page.locator('.resumo')).toHaveCount(0);
  await expect(page.getByRole('button', { name: 'Tentar novamente' })).toBeVisible();
});
