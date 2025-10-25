-- 20250601_1101_DoctorNurse_AddIdxForSickNotes_VisitId.sql
-- Adds an index for sick notes lookup by visit.

SET search_path TO app, public;

CREATE INDEX ix_sick_notes_visit_id ON sick_notes (visit_id);