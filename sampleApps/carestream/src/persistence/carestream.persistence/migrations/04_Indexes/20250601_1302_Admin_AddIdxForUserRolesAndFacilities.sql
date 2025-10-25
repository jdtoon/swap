-- 20250601_1302_Admin_AddIdxForUserRolesAndFacilities.sql
-- Adds indexes for user role and facility assignments.

SET search_path TO app, public;

CREATE INDEX ix_user_roles_user_id ON user_roles (user_id);
CREATE INDEX ix_user_roles_role_id ON user_roles (role_id);
CREATE INDEX ix_user_roles_facility_id ON user_roles (facility_id);
CREATE INDEX ix_user_facilities_user_id ON user_facilities (user_id);
CREATE INDEX ix_user_facilities_facility_id ON user_facilities (facility_id);