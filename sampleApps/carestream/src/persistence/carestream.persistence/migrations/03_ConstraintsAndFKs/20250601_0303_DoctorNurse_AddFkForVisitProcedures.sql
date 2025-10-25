-- 20250601_0303_DoctorNurse_AddFkForVisitProcedures.sql
-- Adds foreign key constraints for the app.visit_procedures table.

SET search_path TO app, public;

ALTER TABLE visit_procedures
    ADD CONSTRAINT fk_visit_procedures_visit_id FOREIGN KEY (visit_id) REFERENCES visits(visit_id),
    ADD CONSTRAINT fk_visit_procedures_procedure_id FOREIGN KEY (procedure_id) REFERENCES procedures(procedure_id),
    ADD CONSTRAINT fk_visit_procedures_performed_by_user_id FOREIGN KEY (performed_by_user_id) REFERENCES users(user_id);