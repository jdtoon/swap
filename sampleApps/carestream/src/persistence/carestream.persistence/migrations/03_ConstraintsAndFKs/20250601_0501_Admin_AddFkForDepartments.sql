-- 20250601_0501_Admin_AddFkForDepartments.sql
-- Adds foreign key constraints for the app.departments table.

SET search_path TO app, public;

ALTER TABLE departments
    ADD CONSTRAINT fk_departments_facility_id FOREIGN KEY (facility_id) REFERENCES facilities(facility_id),
    ADD CONSTRAINT fk_departments_created_by_user_id FOREIGN KEY (created_by_user_id) REFERENCES users(user_id),
    ADD CONSTRAINT fk_departments_updated_by_user_id FOREIGN KEY (updated_by_user_id) REFERENCES users(user_id);