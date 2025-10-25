-- 20250601_0505_Admin_AddFkForRoles.sql
-- Adds foreign key constraints for user tracking in app.roles.

SET search_path TO app, public;

ALTER TABLE roles
    ADD CONSTRAINT fk_roles_created_by_user_id FOREIGN KEY (created_by_user_id) REFERENCES users(user_id),
    ADD CONSTRAINT fk_roles_updated_by_user_id FOREIGN KEY (updated_by_user_id) REFERENCES users(user_id);