-- 20250531_1028_CreatePatientFacilityVisitHistoryTable.sql
-- Creates a table to track which facilities a patient has had visits at.

SET search_path TO app, public;

CREATE TABLE IF NOT EXISTS patient_facility_visits_history (
    patient_id INT NOT NULL,
    facility_id INT NOT NULL,
    first_visit_at TIMESTAMPTZ NOT NULL,
    last_visit_at TIMESTAMPTZ NOT NULL,
    PRIMARY KEY (patient_id, facility_id)
);