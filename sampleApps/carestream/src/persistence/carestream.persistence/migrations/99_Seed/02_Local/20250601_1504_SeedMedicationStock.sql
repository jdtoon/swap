-- 20250601_1504_SeedMedicationStock.sql
-- Seeds initial medication stock levels for facilities.

SET search_path TO app, public;

INSERT INTO medication_stock (medication_id, facility_id, quantity_on_hand, minimum_stock_level, last_updated_at) VALUES
((SELECT medication_id FROM medications WHERE name = 'Amoxicillin' AND strength = '500mg'), (SELECT facility_id FROM facilities WHERE short_code = '2MH'), 150, 50, NOW()),
((SELECT medication_id FROM medications WHERE name = 'Ibuprofen' AND strength = '400mg'), (SELECT facility_id FROM facilities WHERE short_code = '2MH'), 45, 50, NOW()), -- Low stock
((SELECT medication_id FROM medications WHERE name = 'Lisinopril' AND strength = '10mg'), (SELECT facility_id FROM facilities WHERE short_code = '2MH'), 200, 100, NOW()),
((SELECT medication_id FROM medications WHERE name = 'Atorvastatin' AND strength = '20mg'), (SELECT facility_id FROM facilities WHERE short_code = '2MH'), 30, 50, NOW()), -- Low stock
((SELECT medication_id FROM medications WHERE name = 'Metformin' AND strength = '500mg'), (SELECT facility_id FROM facilities WHERE short_code = '2MH'), 180, 100, NOW()),
((SELECT medication_id FROM medications WHERE name = 'Levothyroxine' AND strength = '50mcg'), (SELECT facility_id FROM facilities WHERE short_code = '2MH'), 75, 50, NOW()),

((SELECT medication_id FROM medications WHERE name = 'Amoxicillin' AND strength = '500mg'), (SELECT facility_id FROM facilities WHERE short_code = '1MH'), 100, 50, NOW()),
((SELECT medication_id FROM medications WHERE name = 'Paracetamol' AND strength = '500mg'), (SELECT facility_id FROM facilities WHERE short_code = '1MH'), 300, 100, NOW()),
((SELECT medication_id FROM medications WHERE name = 'Ventolin' AND strength = '100mcg'), (SELECT facility_id FROM facilities WHERE short_code = '1MH'), 20, 10, NOW());