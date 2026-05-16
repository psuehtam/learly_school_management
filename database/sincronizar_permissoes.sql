-- ============================================================
-- LEARLY — SINCRONIZAÇÃO DE PERMISSÕES
-- ============================================================
-- Executar contra um banco JÁ EXISTENTE para:
--   1. Atualizar os templates de perfil com o conjunto completo
--      de permissões definido em setup.sql (versão atual).
--   2. Propagar as permissões adicionadas aos templates para
--      todos os perfis das escolas já criadas (retrocompatibilidade).
--   3. Conceder TODAS as permissões ao perfil Super Admin (SYSTEM).
--
-- É IDEMPOTENTE: pode ser executado várias vezes sem duplicar dados.
-- Usa INSERT IGNORE para não falhar em registros já existentes.
-- ============================================================

USE learly_db;

-- ============================================================
-- 1. RECRIAR OS VÍNCULOS DOS TEMPLATES DE PERFIL
-- ============================================================
-- Limpa e reinsere para garantir que qualquer permissão nova
-- ou removida dos templates seja refletida aqui.

DELETE FROM perfil_permissoes_template;

-- Administrador
INSERT IGNORE INTO perfil_permissoes_template (perfil_template_id, permissao_id)
SELECT pt.id, p.id
FROM perfis_template pt
JOIN permissoes p ON p.nome IN (
  'CRIAR_USUARIO','VISUALIZAR_USUARIO','EDITAR_USUARIO','INATIVAR_USUARIO',
  'GERENCIAR_PERMISSOES_USUARIO',
  'VISUALIZAR_TURMA','CRIAR_TURMA','EDITAR_TURMA','AGENDAR_TURMA','EDITAR_DIAS_TURMA',
  'CONCLUIR_TURMA','INATIVAR_TURMA','CANCELAR_TURMA',
  'VINCULAR_ALUNO_TURMA','DESVINCULAR_ALUNO_TURMA','REMANEJAR_ALUNO',
  'VISUALIZAR_AULA',
  'VISUALIZAR_MATRICULA','CRIAR_MATRICULA','CANCELAR_MATRICULA','EDITAR_MATRICULA',
  'APROVAR_MATRICULA','FINALIZAR_MATRICULA',
  'VISUALIZAR_PRE_ALUNO','CRIAR_PRE_ALUNO','EDITAR_PRE_ALUNO','CANCELAR_PRE_ALUNO',
  'VISUALIZAR_PARCELA',
  'VISUALIZAR_ALUNO','CRIAR_ALUNO','VISUALIZAR_REPOSICAO',
  'VISUALIZAR_LIVRO','CRIAR_LIVRO','EDITAR_LIVRO','INATIVAR_LIVRO',
  'VISUALIZAR_CALENDARIO','VISUALIZAR_DASHBOARD_GERAL','VISUALIZAR_AGENDA_GLOBAL',
  'CRIAR_COMPROMISSO','VISUALIZAR_COMPROMISSOS','EDITAR_COMPROMISSO','EXCLUIR_COMPROMISSO',
  'GERENCIAR_CONFIGURACOES_SISTEMA'
)
WHERE pt.nome = 'Administrador';

-- Professor (somente turmas e minha agenda; sem compromissos/agenda global/calendário escolar)
INSERT IGNORE INTO perfil_permissoes_template (perfil_template_id, permissao_id)
SELECT pt.id, p.id
FROM perfis_template pt
JOIN permissoes p ON p.nome IN (
  'VISUALIZAR_AULA','VISUALIZAR_TURMA'
)
WHERE pt.nome = 'Professor';

-- Comercial
INSERT IGNORE INTO perfil_permissoes_template (perfil_template_id, permissao_id)
SELECT pt.id, p.id
FROM perfis_template pt
JOIN permissoes p ON p.nome IN (
  'VISUALIZAR_PRE_ALUNO','CRIAR_PRE_ALUNO','EDITAR_PRE_ALUNO','CANCELAR_PRE_ALUNO',
  'VISUALIZAR_COMPROMISSOS','CRIAR_COMPROMISSO','EDITAR_COMPROMISSO','EXCLUIR_COMPROMISSO'
)
WHERE pt.nome = 'Comercial';

-- Secretaria
INSERT IGNORE INTO perfil_permissoes_template (perfil_template_id, permissao_id)
SELECT pt.id, p.id
FROM perfis_template pt
JOIN permissoes p ON p.nome IN (
  'VISUALIZAR_MATRICULA','CRIAR_MATRICULA','CANCELAR_MATRICULA','EDITAR_MATRICULA',
  'APROVAR_MATRICULA','FINALIZAR_MATRICULA',
  'VISUALIZAR_ALUNO','CRIAR_ALUNO',
  'VISUALIZAR_PRE_ALUNO',
  'CRIAR_COMPROMISSO','VISUALIZAR_COMPROMISSOS','EDITAR_COMPROMISSO','EXCLUIR_COMPROMISSO'
)
WHERE pt.nome = 'Secretaria';

-- Financeiro
INSERT IGNORE INTO perfil_permissoes_template (perfil_template_id, permissao_id)
SELECT pt.id, p.id
FROM perfis_template pt
JOIN permissoes p ON p.nome IN (
  'VISUALIZAR_PARCELA','VISUALIZAR_MOVIMENTACAO_FINANCEIRA',
  'VISUALIZAR_COMPROMISSOS','CRIAR_COMPROMISSO'
)
WHERE pt.nome = 'Financeiro';

-- Coordenador
INSERT IGNORE INTO perfil_permissoes_template (perfil_template_id, permissao_id)
SELECT pt.id, p.id
FROM perfis_template pt
JOIN permissoes p ON p.nome IN (
  'VISUALIZAR_TURMA','CRIAR_TURMA','EDITAR_TURMA','AGENDAR_TURMA','EDITAR_DIAS_TURMA',
  'CONCLUIR_TURMA','INATIVAR_TURMA','CANCELAR_TURMA',
  'VINCULAR_ALUNO_TURMA','DESVINCULAR_ALUNO_TURMA','REMANEJAR_ALUNO',
  'VISUALIZAR_AULA','VISUALIZAR_REPOSICAO',
  'VISUALIZAR_DASHBOARD_GERAL',
  'VISUALIZAR_LIVRO','CRIAR_LIVRO','EDITAR_LIVRO','INATIVAR_LIVRO',
  'CRIAR_COMPROMISSO','VISUALIZAR_COMPROMISSOS','EDITAR_COMPROMISSO','EXCLUIR_COMPROMISSO'
)
WHERE pt.nome = 'Coordenador';

-- ============================================================
-- 1b. REVOGAR PERMISSÕES INDEVIDAS DO PROFESSOR (escolas existentes)
-- ============================================================
DELETE pp
FROM perfil_permissoes pp
JOIN perfis pf ON pf.id = pp.perfil_id
JOIN escolas e ON e.id = pf.escola_id AND e.codigo_escola <> 'SYSTEM'
JOIN permissoes p ON p.id = pp.permissao_id
WHERE pf.nome = 'Professor'
  AND p.nome IN (
    'VISUALIZAR_CALENDARIO','GERENCIAR_CALENDARIO','EDITAR_EVENTO_CALENDARIO','EXCLUIR_EVENTO_CALENDARIO',
    'VISUALIZAR_AGENDA_GLOBAL','VISUALIZAR_DASHBOARD_GERAL',
    'VISUALIZAR_COMPROMISSOS','CRIAR_COMPROMISSO','EDITAR_COMPROMISSO','EXCLUIR_COMPROMISSO'
  );

-- ============================================================
-- 2. SINCRONIZAR TODAS AS ESCOLAS JÁ CRIADAS
-- ============================================================
-- Para cada perfil de escola cujo nome bate com um template,
-- insere as permissões do template que ainda não existem naquele perfil.
-- INSERT IGNORE garante idempotência.

INSERT IGNORE INTO perfil_permissoes (perfil_id, permissao_id)
SELECT pf.id AS perfil_id, ppt.permissao_id
FROM perfis pf
JOIN escolas e   ON e.id  = pf.escola_id  AND e.codigo_escola <> 'SYSTEM'
JOIN perfis_template pt   ON pt.nome = pf.nome
JOIN perfil_permissoes_template ppt ON ppt.perfil_template_id = pt.id;

-- ============================================================
-- 3. CONCEDER TODAS AS PERMISSÕES AO SUPERADMIN (SYSTEM)
-- ============================================================
-- O perfil Super Admin recebe todas as permissões cadastradas.
-- INSERT IGNORE garante idempotência.

INSERT IGNORE INTO perfil_permissoes (perfil_id, permissao_id)
SELECT p.id, perm.id
FROM perfis p
JOIN escolas e   ON e.id = p.escola_id AND e.codigo_escola = 'SYSTEM'
JOIN permissoes perm ON 1 = 1
WHERE p.nome = 'Super Admin';

-- ============================================================
-- FIM — verificação rápida (opcional, remova se preferir)
-- ============================================================
SELECT
  e.codigo_escola,
  pf.nome   AS perfil,
  COUNT(pp.permissao_id) AS total_permissoes
FROM perfis pf
JOIN escolas e ON e.id = pf.escola_id
JOIN perfil_permissoes pp ON pp.perfil_id = pf.id
GROUP BY e.codigo_escola, pf.nome
ORDER BY e.codigo_escola, pf.nome;
