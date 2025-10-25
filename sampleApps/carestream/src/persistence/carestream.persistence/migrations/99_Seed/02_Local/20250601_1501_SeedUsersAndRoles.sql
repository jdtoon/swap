-- 20250601_1501_SeedUsersAndRoles.sql
-- Seeds initial users and roles, and links them to facilities.

SET search_path TO app, public;

-- Roles (ensure these match your Logto role names if you plan to sync them)
INSERT INTO roles (name, description, created_at) VALUES
('PatientAdmin', 'Manages patient check-in and administration', NOW()),
('Nurse', 'Captures vital signs and assists doctors', NOW()),
('Doctor', 'Performs consultations, diagnoses, and prescribes', NOW()),
('Pharmacist', 'Manages pharmacy operations and dispenses medications', NOW()),
('SystemAdmin', 'Manages system users, settings, and core data', NOW());

-- Users (with placeholder logto_sub values)
-- NOTE: For Logto integration, you would manually register these users in Logto,
-- retrieve their actual 'sub' claim from Logto's admin panel, and update these records.
-- For local dev, you can use these predictable subs, but Logto won't know them unless you configure it.
INSERT INTO users (logto_sub, force_number, rank, first_name, last_name, department, created_at, is_active) VALUES
('logto_sub_patientadmin_1', 'WO12345678', 'Warrant Officer', 'Sarah', 'Mitchell', (SELECT name FROM departments WHERE name='Emergency' LIMIT 1), NOW(), TRUE),
('logto_sub_nurse_1', 'CPT98765432', 'Captain', 'Emily', 'Johnson', (SELECT name FROM departments WHERE name='Emergency' LIMIT 1), NOW(), TRUE),
('logto_sub_doctor_1', 'MAJ11223344', 'Major', 'Themba', 'Zulu', (SELECT name FROM departments WHERE name='Internal Medicine' LIMIT 1), NOW(), TRUE),
('logto_sub_pharmacist_1', 'LTCOL55667788', 'Lieutenant Colonel', 'Abe', 'Moshou', (SELECT name FROM departments WHERE name='Pharmacy' LIMIT 1), NOW(), TRUE),
('logto_sub_sysadmin_1', 'ADM99887766', 'Colonel', 'Chris', 'System', 'IT', NOW(), TRUE),
('logto_sub_patientadmin_2', 'WO11223345', 'Warrant Officer', 'John', 'Rambo', (SELECT name FROM departments WHERE name='Emergency' LIMIT 1), NOW(), TRUE), -- Another Patient Admin
('logto_sub_nurse_2', 'CPT44556677', 'Captain', 'Sipho', 'Dlamini', (SELECT name FROM departments WHERE name='Emergency' LIMIT 1), NOW(), TRUE), -- Another Nurse
('logto_sub_doctor_2', 'MAJ77889900', 'Major', 'Lisa', 'Wong', (SELECT name FROM departments WHERE name='Endocrinology' LIMIT 1), NOW(), TRUE), -- Another Doctor
('logto_sub_pharmacist_2', 'LTCOL12345678', 'Lieutenant Colonel', 'Zandile', 'Nxumalo', (SELECT name FROM departments WHERE name='Pharmacy' LIMIT 1), NOW(), TRUE);

-- User Roles (linking users to roles)
INSERT INTO user_roles (user_id, role_id, facility_id, assigned_at) VALUES
((SELECT user_id FROM users WHERE logto_sub = 'logto_sub_patientadmin_1'), (SELECT role_id FROM roles WHERE name = 'PatientAdmin'), (SELECT facility_id FROM facilities WHERE short_code = '2MH'), NOW()),
((SELECT user_id FROM users WHERE logto_sub = 'logto_sub_nurse_1'), (SELECT role_id FROM roles WHERE name = 'Nurse'), (SELECT facility_id FROM facilities WHERE short_code = '2MH'), NOW()),
((SELECT user_id FROM users WHERE logto_sub = 'logto_sub_doctor_1'), (SELECT role_id FROM roles WHERE name = 'Doctor'), (SELECT facility_id FROM facilities WHERE short_code = '2MH'), NOW()),
((SELECT user_id FROM users WHERE logto_sub = 'logto_sub_pharmacist_1'), (SELECT role_id FROM roles WHERE name = 'Pharmacist'), (SELECT facility_id FROM facilities WHERE short_code = '2MH'), NOW()),
((SELECT user_id FROM users WHERE logto_sub = 'logto_sub_sysadmin_1'), (SELECT role_id FROM roles WHERE name = 'SystemAdmin'), NULL, NOW()), -- System admin is not facility-scoped
((SELECT user_id FROM users WHERE logto_sub = 'logto_sub_patientadmin_2'), (SELECT role_id FROM roles WHERE name = 'PatientAdmin'), (SELECT facility_id FROM facilities WHERE short_code = '1MH'), NOW()),
((SELECT user_id FROM users WHERE logto_sub = 'logto_sub_nurse_2'), (SELECT role_id FROM roles WHERE name = 'Nurse'), (SELECT facility_id FROM facilities WHERE short_code = '1MH'), NOW()),
((SELECT user_id FROM users WHERE logto_sub = 'logto_sub_doctor_2'), (SELECT role_id FROM roles WHERE name = 'Doctor'), (SELECT facility_id FROM facilities WHERE short_code = '1MH'), NOW()),
((SELECT user_id FROM users WHERE logto_sub = 'logto_sub_pharmacist_2'), (SELECT role_id FROM roles WHERE name = 'Pharmacist'), (SELECT facility_id FROM facilities WHERE short_code = '1MH'), NOW()),
((SELECT user_id FROM users WHERE logto_sub = 'logto_sub_doctor_1'), (SELECT role_id FROM roles WHERE name = 'Doctor'), (SELECT facility_id FROM facilities WHERE short_code = '1MH'), NOW()); -- Doctor can be in multiple facilities

-- User Facilities (linking users to facilities and setting a default)
INSERT INTO user_facilities (user_id, facility_id, is_default, created_at) VALUES
((SELECT user_id FROM users WHERE logto_sub = 'logto_sub_patientadmin_1'), (SELECT facility_id FROM facilities WHERE short_code = '2MH'), TRUE, NOW()),
((SELECT user_id FROM users WHERE logto_sub = 'logto_sub_nurse_1'), (SELECT facility_id FROM facilities WHERE short_code = '2MH'), TRUE, NOW()),
((SELECT user_id FROM users WHERE logto_sub = 'logto_sub_doctor_1'), (SELECT facility_id FROM facilities WHERE short_code = '2MH'), TRUE, NOW()),
((SELECT user_id FROM users WHERE logto_sub = 'logto_sub_doctor_1'), (SELECT facility_id FROM facilities WHERE short_code = '1MH'), FALSE, NOW()), -- Doctor 1 works at multiple facilities
((SELECT user_id FROM users WHERE logto_sub = 'logto_sub_pharmacist_1'), (SELECT facility_id FROM facilities WHERE short_code = '2MH'), TRUE, NOW()),
((SELECT user_id FROM users WHERE logto_sub = 'logto_sub_sysadmin_1'), (SELECT facility_id FROM facilities WHERE short_code = '2MH'), TRUE, NOW()),
((SELECT user_id FROM users WHERE logto_sub = 'logto_sub_sysadmin_1'), (SELECT facility_id FROM facilities WHERE short_code = '1MH'), FALSE, NOW()),
((SELECT user_id FROM users WHERE logto_sub = 'logto_sub_sysadmin_1'), (SELECT facility_id FROM facilities WHERE short_code = '3MH'), FALSE, NOW()),
((SELECT user_id FROM users WHERE logto_sub = 'logto_sub_patientadmin_2'), (SELECT facility_id FROM facilities WHERE short_code = '1MH'), TRUE, NOW()),
((SELECT user_id FROM users WHERE logto_sub = 'logto_sub_nurse_2'), (SELECT facility_id FROM facilities WHERE short_code = '1MH'), TRUE, NOW()),
((SELECT user_id FROM users WHERE logto_sub = 'logto_sub_doctor_2'), (SELECT facility_id FROM facilities WHERE short_code = '1MH'), TRUE, NOW()),
((SELECT user_id FROM users WHERE logto_sub = 'logto_sub_pharmacist_2'), (SELECT facility_id FROM facilities WHERE short_code = '1MH'), TRUE, NOW());