-- 20250601_0812_Admin_AddUserFacilityConstraints.sql
-- Adds constraint to ensure a user has only one default facility in app.user_facilities.

SET search_path TO app, public;

-- This uses a partial unique index, which is functionally equivalent to a constraint in PostgreSQL for this purpose.
-- It ensures that for any given user_id, there can be at most one row where is_default is TRUE.
CREATE UNIQUE INDEX uq_user_facilities_default_per_user ON user_facilities (user_id) WHERE is_default = TRUE;