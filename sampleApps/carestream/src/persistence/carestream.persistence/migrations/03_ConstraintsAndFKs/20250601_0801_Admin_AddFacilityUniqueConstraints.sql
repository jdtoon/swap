-- 20250601_0801_Admin_AddFacilityUniqueConstraints.sql
-- Adds unique constraints for name and short_code in app.facilities.

SET search_path TO app, public;

ALTER TABLE facilities ADD CONSTRAINT uq_facilities_name UNIQUE (name);
ALTER TABLE facilities ADD CONSTRAINT uq_facilities_short_code UNIQUE (short_code);