-- 20250601_1300_Admin_AddIdxForFacilitiesDepartmentsWards.sql
-- Adds indexes for administrative entity lookups.

SET search_path TO app, public;

-- Indexes for searching/listing
CREATE INDEX ix_facilities_name ON facilities (name);
CREATE INDEX ix_departments_facility_id ON departments (facility_id);
CREATE INDEX ix_wards_facility_id ON wards (facility_id);
CREATE INDEX ix_wards_department_id ON wards (department_id);