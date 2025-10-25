-- 20250603_1600_SeedSuperAdminUserAndSetup.sql
-- Creates a dedicated super admin user and assigns all roles and facilities.

SET search_path TO app, public;

DO $$
DECLARE
    super_admin_user_id INT;
    temp_logto_sub VARCHAR(100) := 'logto_sub_superadmin_001'; -- Unique sub for this super user
BEGIN
    -- Create the Super Admin User
    INSERT INTO users (logto_sub, force_number, rank, first_name, last_name, department, created_at, is_active)
    VALUES (temp_logto_sub, 'SYSADMIN001', 'Colonel', 'Super', 'Admin', 'IT', NOW(), TRUE)
    ON CONFLICT (logto_sub) DO UPDATE SET is_active = TRUE, updated_at = NOW() -- Handle if already exists
    RETURNING user_id INTO super_admin_user_id;

    RAISE NOTICE 'Super Admin User ID: %', super_admin_user_id;

    -- Assign ALL Roles to the Super Admin User (and link to ALL Facilities)
    INSERT INTO user_roles (user_id, role_id, facility_id, assigned_at, assigned_by_user_id)
    SELECT super_admin_user_id, r.role_id, f.facility_id, NOW(), super_admin_user_id
    FROM roles r, facilities f
    ON CONFLICT (user_id, role_id, facility_id) DO NOTHING;

    -- Also assign SystemAdmin role without facility_id (global)
    INSERT INTO user_roles (user_id, role_id, facility_id, assigned_at, assigned_by_user_id)
    SELECT super_admin_user_id, r.role_id, NULL, NOW(), super_admin_user_id
    FROM roles r WHERE r.name = 'SystemAdmin'
    ON CONFLICT (user_id, role_id, facility_id) DO NOTHING; -- Handles global SystemAdmin role

    -- Link Super Admin to ALL Facilities
    INSERT INTO user_facilities (user_id, facility_id, created_at, created_by_user_id)
    SELECT super_admin_user_id, f.facility_id, NOW(), super_admin_user_id
    FROM facilities f
    ON CONFLICT (user_id, facility_id) DO NOTHING;

    -- Set the first facility as their default
    UPDATE user_facilities
    SET is_default = TRUE
    WHERE user_id = super_admin_user_id
      AND facility_id = (SELECT facility_id FROM facilities ORDER BY facility_id ASC LIMIT 1);

    -- Unset any other defaults for this user (due to initial setup being a loop)
    UPDATE user_facilities
    SET is_default = FALSE
    WHERE user_id = super_admin_user_id
      AND facility_id != (SELECT facility_id FROM facilities ORDER BY facility_id ASC LIMIT 1)
      AND is_default = TRUE;

END $$;