-- 20250601_0803_Admin_AddWardUniqueConstraints.sql
-- Adds unique constraint for ward name within a facility in app.wards.

SET search_path TO app, public;

ALTER TABLE wards ADD CONSTRAINT uq_wards_facility_name UNIQUE (facility_id, name);