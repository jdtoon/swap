-- 20250601_0502_Admin_AddFkForWards.sql
-- Adds foreign key constraints for the app.wards table.

SET search_path TO app, public;

ALTER TABLE wards
    ADD CONSTRAINT fk_wards_facility_id FOREIGN KEY (facility_id) REFERENCES facilities(facility_id),
    ADD CONSTRAINT fk_wards_department_id FOREIGN KEY (department_id) REFERENCES departments(department_id),
    ADD CONSTRAINT fk_wards_created_by_user_id FOREIGN KEY (created_by_user_id) REFERENCES users(user_id),
    ADD CONSTRAINT fk_wards_updated_by_user_id FOREIGN KEY (updated_by_user_id) REFERENCES users(user_id);