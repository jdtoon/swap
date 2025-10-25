-- 20250531_1300_CreateUpdateTimestampFunction.sql
-- Function to update 'updated_at' column.

SET search_path TO app, public;

CREATE OR REPLACE FUNCTION app.fn_update_timestamp()
RETURNS TRIGGER AS $$
BEGIN
   NEW.updated_at = NOW();
   RETURN NEW;
END;
$$ LANGUAGE plpgsql;