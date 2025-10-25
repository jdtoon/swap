-- 20250601_1102_DoctorNurse_AddIdxForVisitDiagnosesProceduresAssessments.sql
-- Adds indexes for visit-related diagnoses, procedures, and assessments.

SET search_path TO app, public;

CREATE INDEX ix_visit_diagnoses_visit_id ON visit_diagnoses (visit_id);
CREATE INDEX ix_visit_procedures_visit_id ON visit_procedures (visit_id);
CREATE INDEX ix_visit_assessments_visit_id ON visit_assessments (visit_id);