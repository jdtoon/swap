-- 20250601_0506_Admin_AddFkForUserRoles.sql
-- Adds foreign key constraints for the app.user_roles table.

SET search_path TO app, public;

ALTER TABLE user_roles
    ADD CONSTRAINT fk_user_roles_user_id FOREIGN KEY (user_id) REFERENCES users(user_id),
    ADD CONSTRAINT fk_user_roles_role_id FOREIGN KEY (role_id) REFERENCES roles(role_id),
    ADD CONSTRAINT fk_user_roles_facility_id FOREIGN KEY (facility_id) REFERENCES facilities(facility_id),
    ADD CONSTRAINT fk_user_roles_assigned_by_user_id FOREIGN KEY (assigned_by_user_id) REFERENCES users(user_id);