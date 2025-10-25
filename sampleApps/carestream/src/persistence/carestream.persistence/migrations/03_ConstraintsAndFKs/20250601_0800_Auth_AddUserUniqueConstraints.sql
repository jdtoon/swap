-- 20250601_0800_Auth_AddUserUniqueConstraints.sql
-- Adds unique constraints for logto_sub and force_number in app.users.

SET search_path TO app, public;

ALTER TABLE users ADD CONSTRAINT uq_users_logto_sub UNIQUE (logto_sub);
ALTER TABLE users ADD CONSTRAINT uq_users_force_number UNIQUE (force_number);