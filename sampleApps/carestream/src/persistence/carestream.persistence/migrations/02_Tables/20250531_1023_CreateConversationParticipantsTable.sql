-- 20250531_1023_CreateConversationParticipantsTable.sql
-- Creates the app.conversation_participants table.

SET search_path TO app, public;

CREATE TABLE IF NOT EXISTS conversation_participants (
    conversation_id INT NOT NULL,
    user_id INT NOT NULL,
    joined_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    last_viewed_at TIMESTAMPTZ NULL,
    PRIMARY KEY (conversation_id, user_id) -- CORRECT COMPOSITE PRIMARY KEY
);