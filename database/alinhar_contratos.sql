-- Alinha tabelas de contratos ao modelo da API (EF).
-- Execute se /api/contratos/templates retornar 500 após setup.sql antigo.
-- Uso: mysql -u ... -p leearly < database/alinhar_contratos.sql

SET @db = DATABASE();

-- contratos_templates: coluna nome
SET @has_nome = (
  SELECT COUNT(*) FROM information_schema.COLUMNS
  WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'contratos_templates' AND COLUMN_NAME = 'nome'
);
SET @sql = IF(
  @has_nome = 0,
  'ALTER TABLE contratos_templates ADD COLUMN nome VARCHAR(200) NULL AFTER escola_id',
  'SELECT 1'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- contratos_templates: coluna conteudo_html
SET @has_html = (
  SELECT COUNT(*) FROM information_schema.COLUMNS
  WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'contratos_templates' AND COLUMN_NAME = 'conteudo_html'
);
SET @sql = IF(
  @has_html = 0,
  'ALTER TABLE contratos_templates ADD COLUMN conteudo_html LONGTEXT NULL AFTER nome',
  'SELECT 1'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- Migrar legado: coluna template -> conteudo_html
SET @has_template = (
  SELECT COUNT(*) FROM information_schema.COLUMNS
  WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'contratos_templates' AND COLUMN_NAME = 'template'
);
SET @sql = IF(
  @has_template > 0,
  'UPDATE contratos_templates SET conteudo_html = template WHERE (conteudo_html IS NULL OR conteudo_html = '''') AND template IS NOT NULL',
  'SELECT 1'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

UPDATE contratos_templates
SET nome = CONCAT('Template v', versao)
WHERE nome IS NULL OR TRIM(nome) = '';

UPDATE contratos_templates
SET conteudo_html = '<p></p>'
WHERE conteudo_html IS NULL OR TRIM(conteudo_html) = '';

ALTER TABLE contratos_templates
  MODIFY nome VARCHAR(200) NOT NULL,
  MODIFY conteudo_html LONGTEXT NOT NULL;

SET @sql = IF(
  @has_template > 0,
  'ALTER TABLE contratos_templates DROP COLUMN template',
  'SELECT 1'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- contratos_gerados: renomear conteudo_gerado -> conteudo_gerado_html
SET @has_old = (
  SELECT COUNT(*) FROM information_schema.COLUMNS
  WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'contratos_gerados' AND COLUMN_NAME = 'conteudo_gerado'
);
SET @has_new = (
  SELECT COUNT(*) FROM information_schema.COLUMNS
  WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'contratos_gerados' AND COLUMN_NAME = 'conteudo_gerado_html'
);
SET @sql = IF(
  @has_old > 0 AND @has_new = 0,
  'ALTER TABLE contratos_gerados CHANGE conteudo_gerado conteudo_gerado_html LONGTEXT NOT NULL',
  'SELECT 1'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
