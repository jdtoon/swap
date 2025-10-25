-- 20250601_1104_DoctorNurse_AddIdxForReferrals_VisitPatientReferredTo.sql
-- Adds indexes for referrals lookup.

SET search_path TO app, public;

CREATE INDEX ix_referrals_visit_id ON referrals (visit_id);
CREATE INDEX ix_referrals_patient_id ON referrals (patient_id);
CREATE INDEX ix_referrals_referred_to_department_id ON referrals (referred_to_department_id);
CREATE INDEX ix_referrals_referred_to_facility_id ON referrals (referred_to_facility_id);