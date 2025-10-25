-- 20250601_0500_Admin_AddFkForFacilities.sql
-- Adds foreign key constraints for user and update tracking in app.facilities.

SET search_path TO app, public;

ALTER TABLE facilities
    ADD CONSTRAINT fk_facilities_created_by_user_id FOREIGN KEY (created_by_user_id) REFERENCES users(user_id),
    ADD CONSTRAINT fk_facilities_updated_by_user_id FOREIGN KEY (updated_by_user_id) REFERENCES users(user_id);