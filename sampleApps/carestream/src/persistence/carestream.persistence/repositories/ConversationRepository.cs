// carestream.persistence.repositories/ConversationRepository.cs
using carestream.core.dtos.messaging;
using carestream.core.infrastructure;
using carestream.core.interfaces.repositories;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace carestream.persistence.repositories
{
    /// <summary>
    /// Repository for managing messaging conversation data persistence.
    /// </summary>
    public class ConversationRepository : BaseRepository, IConversationRepository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConversationRepository"/> class.
        /// </summary>
        /// <param name="configuration">The application configuration.</param>
        /// <param name="logger">The logger for this repository.</param>
        /// <param name="facilityContext">The current facility context.</param>
        public ConversationRepository(IConfiguration configuration, ILogger<ConversationRepository> logger, ICurrentFacilityContext facilityContext)
            : base(configuration, logger, facilityContext)
        {
        }

        /// <inheritdoc/>
        public async Task<ConversationDto?> GetConversationByIdAsync(int conversationId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                SELECT
                    c.conversation_id AS ConversationId,
                    c.name AS Name,
                    c.is_group_chat AS IsGroupChat,
                    c.created_at AS CreatedAt,
                    c.created_by_user_id AS CreatedByUserId,
                    cu.first_name || ' ' || cu.last_name AS CreatedByUserName,
                    c.last_message_at AS LastMessageAt
                FROM app.conversations c
                LEFT JOIN app.users cu ON c.created_by_user_id = cu.user_id
                WHERE c.conversation_id = @ConversationIdParam;";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
                await conn.QueryFirstOrDefaultAsync<ConversationDto>(sql, new { ConversationIdParam = conversationId }, transaction: trans),
                connection, transaction);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<ConversationDto>> GetUserConversationsAsync(int userId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                SELECT
                    c.conversation_id AS ConversationId,
                    c.name AS Name,
                    c.is_group_chat AS IsGroupChat,
                    c.created_at AS CreatedAt,
                    c.created_by_user_id AS CreatedByUserId,
                    cu.first_name || ' ' || cu.last_name AS CreatedByUserName,
                    c.last_message_at AS LastMessageAt,
                    cp.last_viewed_at AS LastViewedAt -- Include for client-side unread status calculation
                FROM app.conversations c
                JOIN app.conversation_participants cp ON c.conversation_id = cp.conversation_id
                LEFT JOIN app.users cu ON c.created_by_user_id = cu.user_id
                WHERE cp.user_id = @UserIdParam
                ORDER BY c.last_message_at DESC;";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
                await conn.QueryAsync<ConversationDto>(sql, new { UserIdParam = userId }, transaction: trans),
                connection, transaction) ?? Enumerable.Empty<ConversationDto>();
        }

        /// <inheritdoc/>
        public async Task<int> CreateConversationAsync(string? name, bool isGroupChat, int createdByUserId, IEnumerable<int> initialParticipantIds, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            if (initialParticipantIds == null || !initialParticipantIds.Any())
            {
                throw new ArgumentException("Initial participants must be provided to create a conversation.", nameof(initialParticipantIds));
            }

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                // Create the conversation
                const string conversationSql = @"
                    INSERT INTO app.conversations (name, is_group_chat, created_at, created_by_user_id, last_message_at)
                    VALUES (@Name, @IsGroupChat, NOW(), @CreatedByUserId, NOW())
                    RETURNING conversation_id;";

                var conversationId = await conn.ExecuteScalarAsync<int>(conversationSql, new { Name = name, IsGroupChat = isGroupChat, CreatedByUserId = createdByUserId }, transaction: trans);

                if (conversationId > 0)
                {
                    // Add participants
                    var participantSql = new StringBuilder("INSERT INTO app.conversation_participants (conversation_id, user_id, joined_at) VALUES ");
                    var participantParams = new DynamicParameters();
                    int i = 0;
                    foreach (var participantId in initialParticipantIds.Distinct()) // Ensure unique participants
                    {
                        participantSql.Append($"(@ConversationId, @UserId{i}, NOW()){(i == initialParticipantIds.Distinct().Count() - 1 ? "" : ",")}");
                        participantParams.Add($"UserId{i}", participantId);
                        i++;
                    }
                    participantParams.Add("ConversationId", conversationId);

                    await conn.ExecuteAsync(participantSql.ToString(), participantParams, transaction: trans);
                }

                return conversationId;
            }, connection, transaction);
        }

        /// <inheritdoc/>
        public async Task<bool> AddParticipantToConversationAsync(int conversationId, int userId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                INSERT INTO app.conversation_participants (conversation_id, user_id, joined_at)
                VALUES (@ConversationId, @UserId, NOW())
                ON CONFLICT (conversation_id, user_id) DO NOTHING; -- Do nothing if participant already exists";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var affectedRows = await conn.ExecuteAsync(sql, new { ConversationId = conversationId, UserId = userId }, transaction: trans);
                return affectedRows == 1; // Returns 1 if inserted, 0 if conflicted
            }, connection, transaction);
        }

        /// <inheritdoc/>
        public async Task<bool> RemoveParticipantFromConversationAsync(int conversationId, int userId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                DELETE FROM app.conversation_participants
                WHERE conversation_id = @ConversationId AND user_id = @UserId;";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var affectedRows = await conn.ExecuteAsync(sql, new { ConversationId = conversationId, UserId = userId }, transaction: trans);
                return affectedRows == 1;
            }, connection, transaction);
        }

        /// <inheritdoc/>
        public async Task<bool> UpdateConversationLastMessageTimestampAsync(int conversationId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                UPDATE app.conversations
                SET last_message_at = NOW()
                WHERE conversation_id = @ConversationId;";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var affectedRows = await conn.ExecuteAsync(sql, new { ConversationId = conversationId }, transaction: trans);
                return affectedRows == 1;
            }, connection, transaction);
        }

        /// <inheritdoc/>
        public async Task<ConversationDto?> GetDirectConversationByParticipantsAsync(int userId1, int userId2, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            // This query finds a conversation that is *not* a group chat and has exactly two participants,
            // where those two participants are userId1 and userId2.
            const string sql = @"
                SELECT
                    c.conversation_id AS ConversationId,
                    c.name AS Name,
                    c.is_group_chat AS IsGroupChat,
                    c.created_at AS CreatedAt,
                    c.created_by_user_id AS CreatedByUserId,
                    cu.first_name || ' ' || cu.last_name AS CreatedByUserName,
                    c.last_message_at AS LastMessageAt
                FROM app.conversations c
                LEFT JOIN app.users cu ON c.created_by_user_id = cu.user_id
                WHERE c.is_group_chat = FALSE
                AND c.conversation_id IN (
                    SELECT cp.conversation_id
                    FROM app.conversation_participants cp
                    WHERE cp.user_id IN (@UserId1, @UserId2)
                    GROUP BY cp.conversation_id
                    HAVING COUNT(DISTINCT cp.user_id) = 2 -- Ensure both users are in it
                    AND (SELECT COUNT(*) FROM app.conversation_participants WHERE conversation_id = cp.conversation_id) = 2 -- Ensure exactly 2 participants
                );";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
                await conn.QueryFirstOrDefaultAsync<ConversationDto>(sql, new { UserId1 = userId1, UserId2 = userId2 }, transaction: trans),
                connection, transaction);
        }
    }
}