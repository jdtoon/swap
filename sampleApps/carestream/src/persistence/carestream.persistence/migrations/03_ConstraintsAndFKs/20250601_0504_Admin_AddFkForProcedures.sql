-- 20250601_0504_Admin_AddFkForProcedures.sql
-- Adds foreign key constraints for user tracking in app.procedures.

SET search_path TO app, public;

ALTER TABLE procedures
    ADD CONSTRAINT fk_procedures_created_by_user_id FOREIGN KEY (created_by_user_id) REFERENCES users(user_id),
    ADD CONSTRAINT fk_procedures_updated_by_user_id FOREIGN KEY (updated_by_user_id) REFERENCES users(user_id);