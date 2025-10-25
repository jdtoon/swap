-- 20250601_1500_SeedAdminBaseData.sql
-- Seeds initial data for facilities, departments, and wards.

SET search_path TO app, public;

-- Facilities
INSERT INTO facilities (name, short_code, address_line1, city, province, phone_number, email_address, is_active, created_at) VALUES
('2 Military Hospital', '2MH', 'Main Road', 'Cape Town', 'Western Cape', '0217996000', 'info.2mh@samhs.mil.za', TRUE, NOW()),
('1 Military Hospital', '1MH', 'Defence Road', 'Pretoria', 'Gauteng', '0123140412', 'info.1mh@samhs.mil.za', TRUE, NOW()),
('3 Military Hospital', '3MH', 'Army Base', 'Bloemfontein', 'Free State', '0514021000', 'info.3mh@samhs.mil.za', TRUE, NOW());

-- Departments (ensure FK to facilities exists and is handled by DbUp)
-- Using SELECT id FROM facilities WHERE... to link to the correct facility_id
INSERT INTO departments (facility_id, name, description, is_active, created_at) VALUES
((SELECT facility_id FROM facilities WHERE short_code = '2MH'), 'Emergency', 'Emergency Department', TRUE, NOW()),
((SELECT facility_id FROM facilities WHERE short_code = '2MH'), 'Cardiology', 'Heart Health Department', TRUE, NOW()),
((SELECT facility_id FROM facilities WHERE short_code = '2MH'), 'Pharmacy', 'Medication Dispensing', TRUE, NOW()),
((SELECT facility_id FROM facilities WHERE short_code = '2MH'), 'Internal Medicine', 'General Adult Medicine', TRUE, NOW()),
((SELECT facility_id FROM facilities WHERE short_code = '2MH'), 'Pediatrics', 'Child Health', TRUE, NOW()),
((SELECT facility_id FROM facilities WHERE short_code = '2MH'), 'Supply Chain', 'Medical Supply Management', TRUE, NOW()),

((SELECT facility_id FROM facilities WHERE short_code = '1MH'), 'Emergency', 'Emergency Department', TRUE, NOW()),
((SELECT facility_id FROM facilities WHERE short_code = '1MH'), 'Orthopedics', 'Bone and Joint Health', TRUE, NOW()),
((SELECT facility_id FROM facilities WHERE short_code = '1MH'), 'Pharmacy', 'Medication Dispensing', TRUE, NOW()),
((SELECT facility_id FROM facilities WHERE short_code = '1MH'), 'Endocrinology', 'Hormone and Metabolism Health', TRUE, NOW()),

((SELECT facility_id FROM facilities WHERE short_code = '3MH'), 'Emergency', 'Emergency Department', TRUE, NOW()),
((SELECT facility_id FROM facilities WHERE short_code = '3MH'), 'General Surgery', 'Surgical Procedures', TRUE, NOW()),
((SELECT facility_id FROM facilities WHERE short_code = '3MH'), 'Pharmacy', 'Medication Dispensing', TRUE, NOW());

-- Wards (ensure FK to facilities and departments exists and is handled by DbUp)
INSERT INTO wards (facility_id, department_id, name, description, is_active, created_at) VALUES
((SELECT facility_id FROM facilities WHERE short_code = '2MH'), (SELECT department_id FROM departments WHERE name = 'Emergency' AND facility_id = (SELECT facility_id FROM facilities WHERE short_code = '2MH')), 'Emergency Ward 1', 'Main Emergency Ward', TRUE, NOW()),
((SELECT facility_id FROM facilities WHERE short_code = '2MH'), (SELECT department_id FROM departments WHERE name = 'Cardiology' AND facility_id = (SELECT facility_id FROM facilities WHERE short_code = '2MH')), 'Cardio Ward 1', 'Cardiac Patients', TRUE, NOW()),
((SELECT facility_id FROM facilities WHERE short_code = '2MH'), NULL, 'General Ward A', 'General medical ward', TRUE, NOW()), -- Ward not linked to specific department
((SELECT facility_id FROM facilities WHERE short_code = '2MH'), NULL, 'Pediatric Ward B', 'Pediatric medical ward', TRUE, NOW()),

((SELECT facility_id FROM facilities WHERE short_code = '1MH'), (SELECT department_id FROM departments WHERE name = 'Emergency' AND facility_id = (SELECT facility_id FROM facilities WHERE short_code = '1MH')), 'Emergency Ward 2', 'Main Emergency Ward 2', TRUE, NOW()),
((SELECT facility_id FROM facilities WHERE short_code = '1MH'), NULL, 'Orthopedic Ward C', 'Orthopedic Patients', TRUE, NOW());