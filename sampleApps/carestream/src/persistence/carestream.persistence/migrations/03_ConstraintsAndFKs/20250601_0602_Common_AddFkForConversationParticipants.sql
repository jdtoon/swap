-- 20250601_0602_Common_AddFkForConversationParticipants.sql
-- Adds foreign key constraints for the app.conversation_participants junction table.

SET search_path TO app, public;

-- Adding Composite Primary Key first, if not already done in schema creation script.
-- If it's already there from 20250531_1023_CreateConversationParticipantsTable.sql, you can remove this line.
-- ALTER TABLE conversation_participants ADD CONSTRAINT pk_conversation_participants PRIMARY KEY (conversation_id, user_id);

ALTER TABLE conversation_participants
    ADD CONSTRAINT fk_conv_participants_conversation_id FOREIGN KEY (conversation_id) REFERENCES conversations(conversation_id),
    ADD CONSTRAINT fk_conv_participants_user_id FOREIGN KEY (user_id) REFERENCES users(user_id);