-- 20250601_0604_Common_AddFkForPatientFacilityVisitHistory.sql
-- Adds foreign key constraints for the app.patient_facility_visits_history table.

SET search_path TO app, public;

-- Adding Primary Key first, if not already done in schema creation script.
-- If it's already there from 20250531_1028_CreatePatientFacilityVisitHistoryTable.sql, you can remove this line.
-- ALTER TABLE patient_facility_visits_history ADD CONSTRAINT pk_patient_facility_visits_history PRIMARY KEY (patient_id, facility_id);

ALTER TABLE patient_facility_visits_history
    ADD CONSTRAINT fk_pat_fac_visits_patient_id FOREIGN KEY (patient_id) REFERENCES patients(patient_id),
    ADD CONSTRAINT fk_pat_fac_visits_facility_id FOREIGN KEY (facility_id) REFERENCES facilities(facility_id);