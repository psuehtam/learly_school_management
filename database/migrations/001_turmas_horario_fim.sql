-- Turmas: horário de término (par com coluna horario no EF).
-- Execute uma vez em bancos criados antes desta alteração:
--   mysql -u root -p learly_db < database/migrations/001_turmas_horario_fim.sql

USE learly_db;

ALTER TABLE turmas
  ADD COLUMN horario_fim TIME NULL AFTER horario;
