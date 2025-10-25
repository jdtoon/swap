-- 20250601_0902_PatientAdmin_AddIdxForVisits_PatientAndStatus.sql
-- Adds indexes for common visit lookups by patient and status.

SET search_path TO app, public;

CREATE INDEX ix_visits_patient_id ON visits (patient_id);
CREATE INDEX ix_visits_status_timestamp ON visits (status, visit_timestamp DESC);
CREATE INDEX ix_visits_facility_id_status_timestamp ON visits (facility_id, status, visit_timestamp DESC); -- For multi-tenant queues