-- 20250601_0810_Admin_AddRoleUniqueConstraints.sql
-- Adds unique constraint for role name in app.roles.

SET search_path TO app, public;

ALTER TABLE roles ADD CONSTRAINT uq_roles_name UNIQUE (name);