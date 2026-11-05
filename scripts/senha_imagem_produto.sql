-- Selecione o banco db_atendimento antes de executar. Não apaga cadastros existentes.
-- O hash é gerado no C# com PasswordHasher<Pessoa>; não use MD5/SHA no SQL.
-- NULL é temporário para cadastros antigos: eles só entram depois de definir uma senha.
SET @senha_existe = (SELECT COUNT(*) FROM information_schema.columns
    WHERE table_schema = DATABASE() AND table_name = 'pessoa' AND column_name = 'senha_hash');
SET @comando_senha = IF(@senha_existe = 0,
    'ALTER TABLE pessoa ADD COLUMN senha_hash VARCHAR(512) NULL',
    'SELECT 1 AS campo_senha_ja_existe');
PREPARE alterar_pessoa FROM @comando_senha;
EXECUTE alterar_pessoa;
DEALLOCATE PREPARE alterar_pessoa;

-- Uma imagem por produto; substituir a foto atualiza a mesma linha.
-- MEDIUMBLOB comporta imagens maiores que os 64 KiB de BLOB comum.
-- A API limita o arquivo a 2 MiB e aceita PNG, JPEG e WebP.
CREATE TABLE IF NOT EXISTS imagem_produto (
    iditem INT NOT NULL,
    nome_arquivo VARCHAR(120) NOT NULL,
    tipo_conteudo VARCHAR(50) NOT NULL,
    dados MEDIUMBLOB NOT NULL,
    versao CHAR(32) NOT NULL,
    atualizado_em DATETIME(6) NOT NULL,
    PRIMARY KEY (iditem),
    CONSTRAINT fk_imagem_produto_produto FOREIGN KEY (iditem)
        REFERENCES produto(iditem) ON DELETE CASCADE
) ENGINE=InnoDB;
