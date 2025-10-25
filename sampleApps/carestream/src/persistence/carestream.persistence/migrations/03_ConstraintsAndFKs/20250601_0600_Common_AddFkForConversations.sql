-- 20250601_0600_Common_AddFkForConversations.sql
-- Adds foreign key constraints for the app.conversations table.

SET search_path TO app, public;

ALTER TABLE conversations
    ADD CONSTRAINT fk_conversations_created_by_user_id FOREIGN KEY (created_by_user_id) REFERENCES users(user_id);