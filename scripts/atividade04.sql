-- Selecione db_atendimento na conexão antes de executar este arquivo.
-- Não apaga tabelas nem registros. Resolve e-mails vazios e cria o índice único.
-- Se a consulta abaixo listar duplicados, corrija esses cadastros antes de continuar.
SELECT LOWER(TRIM(email)) AS email_duplicado, COUNT(*) AS quantidade
FROM pessoa WHERE idpessoa > 0 AND email IS NOT NULL AND TRIM(email) <> ''
GROUP BY LOWER(TRIM(email)) HAVING COUNT(*) > 1;

UPDATE pessoa SET email = NULL WHERE idpessoa > 0 AND email IS NOT NULL AND TRIM(email) = '';
UPDATE pessoa SET email = LOWER(TRIM(email)) WHERE idpessoa > 0 AND email IS NOT NULL;

SET @indice_existe = (SELECT COUNT(*) FROM information_schema.statistics
    WHERE table_schema = DATABASE() AND table_name = 'pessoa' AND index_name = 'ux_pessoa_email');
SET @comando = IF(@indice_existe = 0,
    'CREATE UNIQUE INDEX ux_pessoa_email ON pessoa(email)',
    'SELECT 1 AS indice_ja_existe');
PREPARE criar_indice FROM @comando;
EXECUTE criar_indice;
DEALLOCATE PREPARE criar_indice;
