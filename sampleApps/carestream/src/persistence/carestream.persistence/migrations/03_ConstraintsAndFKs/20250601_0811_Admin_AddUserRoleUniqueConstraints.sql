-- 20250601_0811_Admin_AddUserRoleUniqueConstraints.sql
-- Adds unique constraint for user role assignments, considering facility scope, in app.user_roles.

SET search_path TO app, public;

ALTER TABLE user_roles ADD CONSTRAINT uq_user_role_facility UNIQUE (user_id, role_id, facility_id);