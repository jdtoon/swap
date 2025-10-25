-- 20250601_1000_Vitals_AddIdxForVitalSigns_VisitId.sql
-- Adds an index for vital signs lookup by visit.

SET search_path TO app, public;

CREATE INDEX ix_vital_signs_visit_id ON vital_signs (visit_id);
CREATE INDEX ix_vital_signs_patient_id ON vital_signs (patient_id); -- Also common to get all vitals for a patient