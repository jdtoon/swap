-- 20250601_0601_Common_AddFkForMessages.sql
-- Adds foreign key constraints for the app.messages table.

SET search_path TO app, public;

ALTER TABLE messages
    ADD CONSTRAINT fk_messages_conversation_id FOREIGN KEY (conversation_id) REFERENCES conversations(conversation_id),
    ADD CONSTRAINT fk_messages_sender_user_id FOREIGN KEY (sender_user_id) REFERENCES users(user_id);