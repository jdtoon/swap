-- 20250601_1400_Common_AddIdxForStaffReports.sql
-- Adds indexes for staff reports lookup.

SET search_path TO app, public;

CREATE INDEX ix_staff_reports_author_user_id ON staff_reports (author_user_id);
CREATE INDEX ix_staff_reports_facility_id ON staff_reports (facility_id, created_at DESC);
CREATE INDEX ix_staff_reports_department_id ON staff_reports (department_id, created_at DESC);