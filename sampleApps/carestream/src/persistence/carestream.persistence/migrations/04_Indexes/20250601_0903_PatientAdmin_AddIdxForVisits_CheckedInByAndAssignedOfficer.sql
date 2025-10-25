-- 20250601_0903_PatientAdmin_AddIdxForVisits_CheckedInByAndAssignedOfficer.sql
-- Adds indexes for visits by user assignments.

SET search_path TO app, public;

CREATE INDEX ix_visits_checked_in_by_user_id ON visits (checked_in_by_user_id);
CREATE INDEX ix_visits_assigned_officer_user_id ON visits (assigned_officer_user_id);