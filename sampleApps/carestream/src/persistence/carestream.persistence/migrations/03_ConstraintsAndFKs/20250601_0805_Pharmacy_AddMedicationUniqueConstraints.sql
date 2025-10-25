-- 20250601_0805_Pharmacy_AddMedicationUniqueConstraints.sql
-- Adds unique constraint for medication name, strength, and form in app.medications.

SET search_path TO app, public;

-- Assuming (name, strength, form) should be unique across the entire system.
-- If strength or form can be NULL, you might need to handle uniqueness with IS NOT NULL or consider a different approach for partial uniqueness.
-- For now, this assumes they are all NOT NULL or that NULL values are treated as distinct for uniqueness.
ALTER TABLE medications ADD CONSTRAINT uq_medication_name_strength_form UNIQUE (name, strength, form);