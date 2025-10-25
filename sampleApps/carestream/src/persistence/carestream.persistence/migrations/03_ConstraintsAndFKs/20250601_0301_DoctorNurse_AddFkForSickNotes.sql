-- 20250601_0301_DoctorNurse_AddFkForSickNotes.sql
-- Adds foreign key constraints for the app.sick_notes table.

SET search_path TO app, public;

ALTER TABLE sick_notes
    ADD CONSTRAINT fk_sick_notes_visit_id FOREIGN KEY (visit_id) REFERENCES visits(visit_id),
    ADD CONSTRAINT fk_sick_notes_issued_by_user_id FOREIGN KEY (issued_by_user_id) REFERENCES users(user_id);