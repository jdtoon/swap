-- 20250601_1100_DoctorNurse_AddIdxForPrescriptionItems_VisitId.sql
-- Adds indexes for prescription items lookup by visit and sent status.

SET search_path TO app, public;

CREATE INDEX ix_prescription_items_visit_id ON prescription_items (visit_id);
CREATE INDEX ix_prescription_items_is_sent_to_pharmacy ON prescription_items (is_sent_to_pharmacy);