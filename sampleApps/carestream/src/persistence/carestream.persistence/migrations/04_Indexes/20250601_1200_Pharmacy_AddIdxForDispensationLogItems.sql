-- 20250601_1200_Pharmacy_AddIdxForDispensationLogItems.sql
-- Adds indexes for dispensation log lookups.

SET search_path TO app, public;

CREATE INDEX ix_dispensation_log_items_prescription_item_id ON dispensation_log_items (prescription_item_id);
CREATE INDEX ix_dispensation_log_items_visit_id ON dispensation_log_items (visit_id);
CREATE INDEX ix_dispensation_log_items_medication_id ON dispensation_log_items (medication_id);
CREATE INDEX ix_dispensation_log_items_dispensed_by_user_id ON dispensation_log_items (dispensed_by_user_id);
CREATE INDEX ix_dispensation_log_items_dispensed_at ON dispensation_log_items (dispensed_at DESC);