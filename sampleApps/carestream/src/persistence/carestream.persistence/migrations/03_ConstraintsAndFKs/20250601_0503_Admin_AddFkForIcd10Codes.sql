-- 20250601_0503_Admin_AddFkForIcd10Codes.sql
-- Adds foreign key constraints for user tracking in app.icd10_codes.

SET search_path TO app, public;

ALTER TABLE icd10_codes
    ADD CONSTRAINT fk_icd10_codes_created_by_user_id FOREIGN KEY (created_by_user_id) REFERENCES users(user_id),
    ADD CONSTRAINT fk_icd10_codes_updated_by_user_id FOREIGN KEY (updated_by_user_id) REFERENCES users(user_id);