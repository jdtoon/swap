-- 20250601_1503_SeedMedicalCatalogData.sql
-- Seeds data for medications, ICD-10 codes, and procedures.

SET search_path TO app, public;

-- Medications
INSERT INTO medications (name, strength, form, category, is_active) VALUES
('Amoxicillin', '500mg', 'Capsule', 'Antibiotic', TRUE),
('Ibuprofen', '400mg', 'Tablet', 'NSAID', TRUE),
('Lisinopril', '10mg', 'Tablet', 'Antihypertensive', TRUE),
('Atorvastatin', '20mg', 'Tablet', 'Statin', TRUE),
('Metformin', '500mg', 'Tablet', 'Antidiabetic', TRUE),
('Levothyroxine', '50mcg', 'Tablet', 'Thyroid Hormone', TRUE),
('Paracetamol', '500mg', 'Tablet', 'Analgesic', TRUE),
('Ventolin', '100mcg', 'Inhaler', 'Bronchodilator', TRUE);

-- ICD-10 Codes
INSERT INTO icd10_codes (code, description, category, is_active, created_at) VALUES
('J06.9', 'Acute upper respiratory infection, unspecified', 'Respiratory', TRUE, NOW()),
('K29.70', 'Gastritis, unspecified, without hemorrhage or perforation', 'Digestive', TRUE, NOW()),
('M54.5', 'Low back pain', 'Musculoskeletal', TRUE, NOW()),
('G43.909', 'Migraine, unspecified, not intractable, without status migrainosus', 'Neurological', TRUE, NOW()),
('R51', 'Headache', 'Symptoms', TRUE, NOW()),
('R50.9', 'Fever, unspecified', 'Symptoms', TRUE, NOW()),
('S93.401A', 'Sprain of ankle, unspecified, initial encounter for closed fracture', 'Injury', TRUE, NOW()),
('R10.9', 'Unspecified abdominal pain', 'Symptoms', TRUE, NOW()),
('I10', 'Essential (primary) hypertension', 'Circulatory', TRUE, NOW()),
('E11.9', 'Type 2 diabetes mellitus without complications', 'Endocrine', TRUE, NOW());

-- Procedures
INSERT INTO procedures (code, name, description, category, is_active, created_at) VALUES
('PROC001', 'Basic Physical Examination', 'General health check', 'Diagnostic', TRUE, NOW()),
('PROC002', 'Suture Removal', 'Removal of stitches', 'Minor Surgical', TRUE, NOW()),
('PROC003', 'Wound Dressing Change', 'Changing wound dressing', 'Nursing Care', TRUE, NOW()),
('PROC004', 'Venipuncture', 'Blood draw for lab tests', 'Diagnostic', TRUE, NOW()),
('PROC005', 'Urinalysis', 'Urine sample analysis', 'Diagnostic', TRUE, NOW());