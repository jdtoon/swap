-- 20250601_0507_Admin_AddFkForUserFacilities.sql
-- Adds foreign key constraints for the app.user_facilities junction table.

SET search_path TO app, public;

-- Adding Primary Key first, if not already done in schema creation script.
-- If it's already there from 20250531_1026_CreateUserFacilitiesTable.sql, you can remove this line.
-- ALTER TABLE user_facilities ADD CONSTRAINT pk_user_facilities PRIMARY KEY (user_id, facility_id);

ALTER TABLE user_facilities
    ADD CONSTRAINT fk_user_facilities_user_id FOREIGN KEY (user_id) REFERENCES users(user_id),
    ADD CONSTRAINT fk_user_facilities_facility_id FOREIGN KEY (facility_id) REFERENCES facilities(facility_id),
    ADD CONSTRAINT fk_user_facilities_created_by_user_id FOREIGN KEY (created_by_user_id) REFERENCES users(user_id);