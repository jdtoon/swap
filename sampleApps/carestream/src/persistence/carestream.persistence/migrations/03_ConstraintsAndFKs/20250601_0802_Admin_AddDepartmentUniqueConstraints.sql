-- 20250601_0802_Admin_AddDepartmentUniqueConstraints.sql
-- Adds unique constraint for department name within a facility in app.departments.

SET search_path TO app, public;

ALTER TABLE departments ADD CONSTRAINT uq_departments_facility_name UNIQUE (facility_id, name);