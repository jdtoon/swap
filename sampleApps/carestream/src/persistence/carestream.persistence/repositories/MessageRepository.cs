using carestream.core.dtos.messaging; // For MessageDto, CreateMessageInputDto
using carestream.core.infrastructure; // For ICurrentFacilityContext
using carestream.core.interfaces.repositories;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq; // For Enumerable.Empty()
using System.Threading.Tasks;

namespace carestream.persistence.repositories
{
    /// <summary>
    /// Repository for managing individual messages within conversations.
    /// </summary>
    public class MessageRepository : BaseRepository, IMessageRepository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageRepository"/> class.
        /// </summary>
        /// <param name="configuration">The application configuration.</param>
        /// <param name="logger">The logger for this repository.</param>
        /// <param name="facilityContext">The current facility context.</param>
        public MessageRepository(IConfiguration configuration, ILogger<MessageRepository> logger, ICurrentFacilityContext facilityContext)
            : base(configuration, logger, facilityContext)
        {
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<MessageDto>> GetConversationMessagesAsync(int conversationId, int limit = 100, int offset = 0, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                SELECT
                    m.message_id AS MessageId,
                    m.conversation_id AS ConversationId,
                    m.sender_user_id AS SenderUserId,
                    u.first_name || ' ' || u.last_name AS SenderUserName,
                    u.rank AS SenderUserRank,
                    m.content AS Content,
                    m.sent_at AS SentAt,
                    m.is_read AS IsRead -- This 'is_read' is typically per-message, not per-user-per-message.
                                        -- For per-user read status, conversation_participants.last_viewed_at is used.
                FROM app.messages m
                JOIN app.users u ON m.sender_user_id = u.user_id
                WHERE m.conversation_id = @ConversationIdParam
                ORDER BY m.sent_at ASC
                LIMIT @Limit OFFSET @Offset;";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
                await conn.QueryAsync<MessageDto>(sql, new { ConversationIdParam = conversationId, Limit = limit, Offset = offset }, transaction: trans),
                connection, transaction) ?? Enumerable.Empty<MessageDto>();
        }

        /// <inheritdoc/>
        public async Task<int> CreateMessageAsync(CreateMessageInputDto messageData, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                INSERT INTO app.messages (
                    conversation_id, sender_user_id, content, sent_at, is_read
                ) VALUES (
                    @ConversationId, @SenderUserId, @Content, NOW(), FALSE
                )
                RETURNING message_id;";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
                await conn.ExecuteScalarAsync<int>(sql, messageData, transaction: trans),
                connection, transaction);
        }

        /// <inheritdoc/>
        public async Task<bool> MarkMessageAsReadAsync(int messageId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                UPDATE app.messages
                SET is_read = TRUE
                WHERE message_id = @MessageIdParam AND is_read = FALSE;"; // Only update if not already read

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var affectedRows = await conn.ExecuteAsync(sql, new { MessageIdParam = messageId }, transaction: trans);
                return affectedRows == 1;
            }, connection, transaction);
        }

        /// <inheritdoc/>
        public async Task<bool> UpdateParticipantLastViewedAtAsync(int conversationId, int userId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                UPDATE app.conversation_participants
                SET last_viewed_at = NOW()
                WHERE conversation_id = @ConversationIdParam AND user_id = @UserIdParam;";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var affectedRows = await conn.ExecuteAsync(sql, new { ConversationIdParam = conversationId, UserIdParam = userId }, transaction: trans);
                return affectedRows == 1;
            }, connection, transaction);
        }

        /// <inheritdoc/>
        public async Task<int> GetUnreadMessagesCountInConversationAsync(int conversationId, int userId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            // This query calculates unread messages based on the user's last_viewed_at timestamp in the conversation_participants table.
            const string sql = @"
                SELECT COUNT(m.message_id)
                FROM app.messages m
                JOIN app.conversation_participants cp ON m.conversation_id = cp.conversation_id
                WHERE m.conversation_id = @ConversationIdParam
                  AND cp.user_id = @UserIdParam
                  AND m.sent_at > COALESCE(cp.last_viewed_at, '1900-01-01'::timestamptz); -- Compare against last_viewed_at, or a very old date if never viewed
            ";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
                await conn.QuerySingleOrDefaultAsync<int>(sql, new { ConversationIdParam = conversationId, UserIdParam = userId }, transaction: trans),
                connection, transaction);
        }
    }
}