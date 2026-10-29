import { expect, test } from '@playwright/test';

test('loja: filtros, carrinho, cadastro, compra, privacidade e cancelamento pela equipe', async ({
  page,
  browser,
}) => {
  const codigo = String(Date.now()).slice(-8);
  const api = page.request;
  await api.post('/api/Sessao', { data: { cpf: 90000000001, dataNascimento: '1990-01-01' } });
  const categoria = await (
    await api.post('/api/Categoriums', { data: { categoria: `Beleza ${codigo}` } })
  ).json();
  const produtoNome = `Creme ${codigo}`;
  const servicoNome = `Massagem ${codigo}`;
  const clienteNome = `Cliente Loja ${codigo}`;
  const produto = await (
    await api.post('/api/Items', {
      data: {
        idCategoria: categoria.idcategoria,
        item: produtoNome,
        descricaoItem: 'Cuidado diário',
        tipoItem: 'P',
        preco: 20,
        produto: { codigoPreco: 0, precoVenda: 20, unidade: 'UN' },
      },
    })
  ).json();
  const servico = await (
    await api.post('/api/Items', {
      data: { idCategoria: categoria.idcategoria, item: servicoNome, tipoItem: 'S', preco: 50 },
    })
  ).json();
  await api.post('/api/EntradaProduto', {
    data: {
      idItem: produto.idItem,
      dataEntradaProduto: '2026-10-29',
      quantidadeEntrada: 5,
      unidade: 'UN',
      valorUnitarioEntrada: 20,
    },
  });
  await api.delete('/api/Sessao');
  await page.goto('/loja');
  await page.getByLabel('Categoria', { exact: true }).selectOption({ label: categoria.categoria });
  await page.getByLabel('Tipo', { exact: true }).selectOption('P');
  await page.getByLabel('Pesquisar por nome').fill('Creme');
  await page.getByRole('button', { name: 'Pesquisar', exact: true }).click();
  await expect(page.locator('.produto-card')).toHaveCount(1);
  await page.getByRole('heading', { name: produtoNome }).getByRole('link').click();
  await expect(page.getByRole('heading', { name: produtoNome })).toBeVisible();
  await page.getByRole('button', { name: 'Adicionar ao carrinho' }).click();
  await page.goto(`/loja?idCategoria=${categoria.idcategoria}`);
  await expect(page.locator('.produto-card')).toHaveCount(2);
  await page.getByRole('button', { name: `Adicionar ${produtoNome}`, exact: true }).click();
  await page.getByRole('button', { name: `Adicionar ${servicoNome}`, exact: true }).click();
  await page.getByRole('link', { name: 'Carrinho (3)' }).click();
  await page.getByLabel(`Quantidade de ${produtoNome}`).fill('6');
  await expect(page.locator('.erro-campo')).toContainText('estoque disponível');
  await expect(page.getByRole('button', { name: 'Confirmar pedido' })).toBeDisabled();
  await page.getByLabel(`Quantidade de ${produtoNome}`).fill('3');
  await expect(page.locator('.total')).toContainText('110,00');
  await page
    .getByRole('row')
    .filter({ hasText: servicoNome })
    .getByRole('button', { name: 'Remover' })
    .click();
  await expect(page.locator('.total')).toContainText('60,00');
  await page.goto(`/loja?idCategoria=${categoria.idcategoria}&tipo=S`);
  await page.getByRole('button', { name: `Adicionar ${servicoNome}`, exact: true }).click();
  await page.goto('/loja/carrinho');
  await expect(page.getByLabel(`Quantidade de ${produtoNome}`)).toHaveValue('3');
  await page.reload();
  await expect(page.locator('.total')).toContainText('110,00');
  await page.getByRole('button', { name: 'Confirmar pedido' }).click();
  await expect(page.getByRole('heading', { name: 'Identifique-se' })).toBeVisible();
  await page.getByRole('link', { name: 'Criar meu cadastro' }).click();
  await page.getByLabel('Nome completo').fill(clienteNome);
  await page.getByLabel('CPF').fill(`850${codigo}`);
  await page.getByLabel('Data de nascimento').fill('2000-01-15');
  await page.getByLabel('E-mail').fill('invalido');
  await page.getByLabel('Telefone').focus();
  await expect(page.getByRole('alert')).toContainText('e-mail válido');
  await page.getByLabel('E-mail').fill(`loja${codigo}@example.com`);
  await page.locator('#cep0').fill('01001000');
  await page.locator('#numero0').fill('10');
  await page.getByRole('button', { name: 'Salvar cadastro' }).click();
  await expect(page.getByRole('heading', { name: 'Carrinho de compras' })).toBeVisible();
  await expect(page.getByText(clienteNome, { exact: true }).first()).toBeVisible();
  await page.screenshot({ path: 'test-results/carrinho.png', fullPage: true });
  await page.getByRole('button', { name: 'Confirmar pedido' }).click();
  await expect(page.getByRole('status')).toContainText('Pedido confirmado');
  await expect(page.locator('.detalhes')).toContainText('110,00');
  const pedido = (await (await api.get('/api/Atendimento/meus')).json())[0];
  expect(pedido.idAtendimento).toBeGreaterThan(0);
  expect((await api.get('/api/Atendimento/all')).status()).toBe(403);

  const outroContexto = await browser.newContext({ baseURL: 'http://127.0.0.1:4200' });
  const outraPessoa = await outroContexto.request.post('/api/PessoaEndereco', {
    data: {
      nome: `Outro ${codigo}`,
      cpf: Number(`860${codigo}`),
      dataNascimento: '2000-01-15',
      email: `outro${codigo}@example.com`,
      enderecos: [{ cep: '49000000' }],
    },
  });
  expect(outraPessoa.ok()).toBeTruthy();
  await outroContexto.request.post('/api/Sessao', {
    data: { cpf: Number(`860${codigo}`), dataNascimento: '2000-01-15' },
  });
  expect(
    (await outroContexto.request.get(`/api/Atendimento/meus/${pedido.idAtendimento}`)).status(),
  ).toBe(404);
  const outraPagina = await outroContexto.newPage();
  await outraPagina.goto('/loja/pedidos');
  await expect(outraPagina.getByText('Você ainda não possui pedidos.')).toBeVisible();
  await outroContexto.close();

  const equipe = await browser.newContext({ baseURL: 'http://127.0.0.1:4200' });
  await equipe.request.post('/api/Sessao', {
    data: { cpf: 90000000001, dataNascimento: '1990-01-01' },
  });
  const admin = await equipe.newPage();
  admin.on('dialog', (dialog) => dialog.accept());
  await admin.goto('/atendimentos');
  await admin.getByLabel('Consultar por ID').fill(String(pedido.idAtendimento));
  await admin.getByRole('button', { name: 'Consultar', exact: true }).click();
  await expect(admin.locator('.detalhes')).toContainText('Responsavel');
  await admin.getByRole('button', { name: 'Fechar detalhes' }).click();
  await admin.getByLabel('Filtrar pedidos por situação').selectOption('A');
  await admin
    .getByRole('row')
    .filter({ hasText: clienteNome })
    .getByRole('button', { name: 'Processar' })
    .click();
  await admin.getByLabel('Filtrar pedidos por situação').selectOption('P');
  await expect(admin.getByRole('row').filter({ hasText: clienteNome })).toContainText(
    'Em processamento',
  );
  await admin
    .getByRole('row')
    .filter({ hasText: clienteNome })
    .getByRole('button', { name: 'Cancelar pedido' })
    .click();
  await expect(admin.getByRole('status')).toContainText('Situação do pedido atualizada');
  const saldo = await (await equipe.request.get(`/api/Estoque/${produto.idItem}`)).json();
  expect(saldo.quantidadeEstoque).toBe(5);
  await admin.goto('/estoque');
  await admin.getByLabel('Consultar por ID').fill(String(produto.idItem));
  await admin.getByRole('button', { name: 'Consultar', exact: true }).click();
  await expect(admin.getByRole('row').filter({ hasText: produtoNome })).toContainText('5');
  await admin.getByLabel('Consultar por ID').fill('99999999');
  await admin.getByRole('button', { name: 'Consultar', exact: true }).click();
  await expect(admin.getByRole('alert')).toContainText('não localizado');
  await page.goto(`/loja/pedidos/${pedido.idAtendimento}`);
  await expect(page.locator('.detalhes')).toContainText('Cancelado');
  await expect(page.locator('.detalhes tbody tr')).toHaveCount(2);
  await page.screenshot({ path: 'test-results/meu-pedido.png', fullPage: true });
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto(`/loja?idCategoria=${categoria.idcategoria}`);
  await expect(page.locator('.produto-card')).toHaveCount(2);
  expect(
    await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth),
  ).toBeTruthy();
  await page.screenshot({ path: 'test-results/loja-mobile.png', fullPage: true });
  await page.getByRole('button', { name: 'Sair', exact: true }).click();
  await expect(page.getByRole('link', { name: 'Identificar-me', exact: true })).toBeVisible();
  expect((await api.get('/api/Atendimento/meus')).status()).toBe(401);

  await equipe.request.delete(`/api/Atendimento/${pedido.idAtendimento}`);
  const entradas = await (await equipe.request.get('/api/EntradaProduto/all')).json();
  for (const entrada of entradas.filter((e: any) => e.idItem === produto.idItem))
    await equipe.request.delete(`/api/EntradaProduto/${entrada.idEntradaProduto}`);
  await equipe.request.delete(`/api/Items/${produto.idItem}`);
  await equipe.request.delete(`/api/Items/${servico.idItem}`);
  await equipe.request.delete(`/api/Categoriums/${categoria.idcategoria}`);
  const pessoas = await (await equipe.request.get('/api/PessoaEndereco/all')).json();
  for (const pessoa of pessoas.filter((p: any) => p.nome.includes(codigo)))
    await equipe.request.delete(`/api/PessoaEndereco/${pessoa.idpessoa}`);
  await equipe.close();
});
