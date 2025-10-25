-- 20250601_1401_Common_AddIdxForMessagesAndConversations.sql
-- Adds indexes for messaging features.

SET search_path TO app, public;

CREATE INDEX ix_messages_conversation_id_sent_at ON messages (conversation_id, sent_at DESC);
CREATE INDEX ix_messages_sender_user_id ON messages (sender_user_id);
CREATE INDEX ix_conversation_participants_user_id ON conversation_participants (user_id);