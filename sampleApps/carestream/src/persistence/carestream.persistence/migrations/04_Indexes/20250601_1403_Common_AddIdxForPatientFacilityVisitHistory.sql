-- 20250601_1403_Common_AddIdxForPatientFacilityVisitHistory.sql
-- Adds indexes for patient facility visit history lookup.

SET search_path TO app, public;

CREATE INDEX ix_patient_facility_visits_history_patient_id ON patient_facility_visits_history (patient_id);
CREATE INDEX ix_patient_facility_visits_history_facility_id ON patient_facility_visits_history (facility_id);
CREATE INDEX ix_patient_facility_visits_history_last_visit_at ON patient_facility_visits_history (last_visit_at DESC);