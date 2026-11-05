import { expect, test } from '@playwright/test';

const senha = 'Teste!12345';
const png = Buffer.from(
  'iVBORw0KGgoAAAANSUhEUgAAAFAAAABQCAIAAAABc2X6AAAAeElEQVR4nO3PAQ3AIADAMEAhSlCHwLv4k71VsM1z9/iT9XXA2wzXGa4zXGe4znCd4TrDdYbrDNcZrjNcZ7jOcJ3hOsN1husM1xmuM1xnuM5wneE6w3WG6wzXGa4zXGe4znCd4TrDdYbrDNcZrjNcZ7jOcJ3hOsN1D7LmAhILoA32AAAAAElFTkSuQmCC',
  'base64',
);

test('login por CPF/e-mail e imagem do produto com substituição, remoção e recuperação de falha', async ({
  page,
  browser,
}) => {
  const codigo = String(Date.now()).slice(-8);
  const nome = `Imagem E2E ${codigo}`;
  page.on('dialog', (d) => d.accept());
  await page.goto('/itens');
  await expect(page.getByRole('heading', { name: 'Identifique-se' })).toBeVisible();
  await page.getByLabel('E-mail ou CPF').fill('90000000001');
  await page.getByLabel('Senha', { exact: false }).fill('incorreta');
  await page.getByRole('button', { name: 'Entrar', exact: true }).click();
  await expect(
    page.getByRole('alert').filter({ hasText: 'E-mail/CPF ou senha inválidos' }),
  ).toBeVisible();
  await page.getByLabel('Senha', { exact: false }).fill(senha);
  await page.getByRole('button', { name: 'Entrar', exact: true }).click();
  await expect(
    page.getByRole('heading', { name: 'Produtos e serviços', exact: true }),
  ).toBeVisible();

  const categoria = await (
    await page.request.post('/api/Categoriums', { data: { categoria: `Fotos ${codigo}` } })
  ).json();
  await page.reload();
  await page.getByRole('button', { name: 'Novo item', exact: true }).click();
  await page.getByLabel('Nome do item').fill(nome);
  await page.locator('#categoria').selectOption({ label: categoria.categoria });
  await page.locator('#tipo').selectOption('P');
  await page.getByLabel('Custo / preço base').fill('10');
  await page.getByLabel('Preço de venda').fill('10');
  await page
    .getByLabel('Imagem do produto')
    .setInputFiles({ name: 'falso.svg', mimeType: 'image/svg+xml', buffer: Buffer.from('<svg/>') });
  await expect(page.getByRole('alert')).toContainText('PNG, JPEG ou WebP');
  await page
    .getByLabel('Imagem do produto')
    .setInputFiles({ name: 'produto.png', mimeType: 'image/png', buffer: png });
  await expect(page.locator('.previa-produto')).toBeVisible();
  let falhou = false;
  await page.route('**/api/Items/*/imagem', async (route) => {
    if (route.request().method() === 'PUT' && !falhou) {
      falhou = true;
      await route.fulfill({
        status: 500,
        contentType: 'application/json',
        body: JSON.stringify({ mensagem: 'Falha simulada no envio.' }),
      });
    } else await route.continue();
  });
  await page.getByRole('button', { name: 'Salvar item', exact: true }).click();
  await expect(page.getByRole('alert')).toContainText('O produto foi salvo, mas a imagem não');
  await expect(page.getByRole('heading', { name: 'Editar item', exact: true })).toBeVisible();
  let itens = await (await page.request.get('/api/Items', { params: { nome } })).json();
  expect(itens).toHaveLength(1);
  const id = itens[0].idItem;
  await page.getByRole('button', { name: 'Salvar item', exact: true }).click();
  await expect(page.getByRole('status')).toContainText('Item salvo com sucesso');
  await expect(page.getByRole('row').filter({ hasText: nome }).locator('img')).toBeVisible();
  itens = await (await page.request.get('/api/Items', { params: { nome } })).json();
  expect(itens).toHaveLength(1);
  const urlAntiga = itens[0].imagemUrl;
  const resposta = await page.request.get(urlAntiga);
  expect(resposta.headers()['content-type']).toContain('image/png');
  expect(await resposta.body()).toEqual(png);

  const visitante = await browser.newContext({ baseURL: 'http://127.0.0.1:4200' });
  const loja = await visitante.newPage();
  await loja.goto(`/loja?nome=${encodeURIComponent(nome)}`);
  const foto = loja.locator('.produto-card img');
  await expect(foto).toHaveAttribute('src', /\/api\/Items\/.*\/imagem\?v=/);
  await expect
    .poll(() => foto.evaluate((i: HTMLImageElement) => i.complete && i.naturalWidth > 0))
    .toBeTruthy();
  await loja.getByRole('heading', { name: nome }).getByRole('link').click();
  await expect(loja.locator('.detalhe-produto img')).toHaveAttribute('src', urlAntiga);
  await loja.screenshot({ path: 'test-results/produto-com-imagem.png', fullPage: true });

  const linha = page.getByRole('row').filter({ hasText: nome });
  await linha.getByRole('button', { name: 'Editar', exact: true }).click();
  await page
    .getByLabel('Imagem do produto')
    .setInputFiles({ name: 'outra.png', mimeType: 'image/png', buffer: png });
  await page.getByRole('button', { name: 'Salvar item', exact: true }).click();
  await expect(page.getByRole('status')).toContainText('Item salvo com sucesso');
  const atualizado = await (await page.request.get(`/api/Items/${id}`)).json();
  expect(atualizado.imagemUrl).not.toBe(urlAntiga);
  await linha.getByRole('button', { name: 'Editar', exact: true }).click();
  await page.getByRole('button', { name: 'Remover imagem atual', exact: true }).click();
  await expect(page.getByRole('status')).toContainText('Imagem removida');
  expect((await (await page.request.get(`/api/Items/${id}`)).json()).imagemUrl).toBeNull();
  await loja.reload();
  await expect(loja.locator('.detalhe-produto img')).toHaveAttribute('src', '/images/produto.svg');
  await visitante.close();

  // Cria um funcionário por sua rota protegida, depois verifica o login por e-mail na tela.
  const email = `equipe${codigo}@example.com`;
  const funcionario = await page.request.post('/api/Funcionario', {
    data: {
      nome: `Equipe senha ${codigo}`,
      cpf: Number(`870${codigo}`),
      dataNascimento: '2000-01-01',
      email,
      senha,
      cargo: 'Atendente',
      dataAdmissao: '2026-01-01',
      enderecos: [{ cep: '49000000' }],
    },
  });
  expect(funcionario.ok()).toBeTruthy();
  await page.request.delete('/api/Sessao');
  await page.goto('/itens');
  await expect(page.getByRole('heading', { name: 'Identifique-se' })).toBeVisible();
  await page.getByLabel('E-mail ou CPF').fill(email.toUpperCase());
  await page.getByLabel('Senha', { exact: false }).fill(senha);
  await page.getByRole('button', { name: 'Entrar', exact: true }).click();
  await expect(
    page.getByRole('heading', { name: 'Produtos e serviços', exact: true }),
  ).toBeVisible();
  expect(await page.evaluate(() => JSON.stringify(localStorage))).not.toContain(senha);
});
